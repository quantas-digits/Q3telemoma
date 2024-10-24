import time, json
import numpy as np
from component import Component
from telemoma.human_interface.teleop_core import BaseTeleopInterface, TeleopAction, TeleopObservation
from telemoma.utils.general_utils import run_threaded_command
from telemoma.utils.transformations import quat_diff, quat_to_euler, rmat_to_quat, quat_to_rmat
import threading
from utils.read_configs import read_json
from server.OculusReaderServer import OculusReaderServer

def vec_to_reorder_mat(vec):
    X = np.zeros((len(vec), len(vec)))
    for i in range(X.shape[0]):
        ind = int(abs(vec[i])) - 1
        X[i, ind] = np.sign(vec[i])
    return X

def vec_to_reorder_pos(vec, order):
    return np.array([np.sign(i)*vec[abs(i)-1] for i in order])

class OculusPolicy(BaseTeleopInterface, Component):
    def __init__(
        self,
        mp_share_dict,
        max_lin_vel: float = 1,
        max_rot_vel: float = 1,
        max_gripper_vel: float = 1,
        spatial_coeff: float = 1,
        pos_action_gain: float = 5,
        rot_action_gain: float = 2,
        gripper_action_gain: float = 3,
        rmat_reorder: list = [3, 1, 2, 4],# better not flip the axis here, do it on the generated action 
        pos_action_sign = np.array([1, -1, 1]),
        quat_action_sign = np.array([-1, 1, -1, 1]),
        *args,
        **kwargs
    ) -> None:
        super().__init__(*args, **kwargs)

        self.vr_to_global_mat = {'right': np.eye(4), 'left': np.eye(4)}
        self.max_lin_vel = max_lin_vel
        self.max_rot_vel = max_rot_vel
        self.max_gripper_vel = max_gripper_vel
        self.spatial_coeff = spatial_coeff
        self.pos_action_gain = pos_action_gain
        self.rot_action_gain = rot_action_gain
        self.gripper_action_gain = gripper_action_gain
        self.pos_action_sign = pos_action_sign
        self.quat_action_sign = quat_action_sign
        self.global_to_env_mat = vec_to_reorder_mat(rmat_reorder)
        self.reset_orientation = {'right': True, 'left': True}
        self.target_gripper = {'right': 1, 'left': 1}
        self.update_internal_state_flag = True,
        self.mp_share_dict = mp_share_dict
        self.reset_state()

    def stream(self):
        # launch a new thread to read and update signal.
        # self.server.stream()
        # print("server is Listening......")
        # self.reading_process = Process(target=self._update_internal_state, args=())
        # self.reading_process.start()
        # print("Updating controller states......")

        # new a thread to update the contrller data

        self.reading_thread = run_threaded_command(self._update_internal_state)
        
    def destory(self):
        self.update_internal_state_flag = False
        if hasattr(self, "reading_thread") and self.reading_thread.is_alive():
            self.reading_process.join()  # Wait for the thread to finish
        self.reset_state()

    def start(self):
        self.stream()
        
    def stop(self):
        self.destory()
    
    def reset_state(self) -> None:
        self._state = {
            'right': {
                "poses": None,
                "movement_enabled": False,
                "controller_on": True,
                "prev_gripper": False,
                "gripper_toggle": False,
            },

            'left': {
                "poses": None,
                "movement_enabled": False,
                "controller_on": True,
                "prev_gripper": False,
                "gripper_toggle": False,
            },
            'buttons': {}
        }
        self.update_sensor = {'right': True, 'left': True}
        self.reset_origin = {'right': True, 'left': True}
        self.robot_origin = {'right': None, 'left': None}
        self.vr_origin = {'right': None, 'left': None}
        self.vr_state = {'right': None, 'left': None}

    def _update_internal_state(self, num_wait_sec=5, hz=50):
        last_read_time = time.time()
        
        while self.update_internal_state_flag:
            time.sleep(1 / hz)
            # Read Controller
            time_since_read = time.time() - last_read_time
            latest_state = self.mp_share_dict["latest_pose"]
            poses, buttons = self.prase_message(latest_state)

            if poses == {}:
                self.reset_state() # reset state if no control signal, which can avoid the drifting of action.
                continue

            # Determine Control Pipeline #
            for arm in ['left', 'right']:
                button_G = 'rightIsGrip' if arm=='right' else 'leftIsGrip'
                button_J = 'RJs' if arm=='right' else 'LJs'
                controller_id = 'r' if arm=='right' else 'l'

                if controller_id not in poses:
                    continue
                self._state[arm]["controller_on"] = time_since_read < num_wait_sec

                toggled = self._state[arm]["movement_enabled"] != buttons[button_G]
                self.update_sensor[arm] = self.update_sensor[arm] or buttons[button_G]
                self.reset_orientation[arm] = self.reset_orientation[arm] or buttons[button_J]
                self.reset_origin[arm] = self.reset_origin[arm] or toggled

                # Save Info #
                self._state[arm]["poses"] = poses[controller_id]
                self._state["buttons"] = buttons
                self._state[arm]["movement_enabled"] = buttons[button_G]
                self._state[arm]["controller_on"] = True

                new_gripper = buttons[f"{arm}Trig"][0] > 0.5
                self._state[arm]["gripper_toggle"] = ((not self._state[arm]["prev_gripper"]) and new_gripper) or self._state[arm]["gripper_toggle"]
                self._state[arm]["prev_gripper"] = new_gripper

                last_read_time = time.time()

                stop_updating = self._state["buttons"][button_J] or self._state[arm]["movement_enabled"]
                if self.reset_orientation[arm]:
                    rot_mat = np.asarray(self._state[arm]["poses"])
                    if stop_updating:
                        self.reset_orientation[arm] = False
                    # try to invert the rotation matrix, if not possible, then just use the identity matrix                
                    try:
                        rot_mat = np.linalg.inv(rot_mat)
                    except:
                        print(f"exception for rot mat: {rot_mat}")
                        rot_mat = np.eye(4)
                        self.reset_orientation[arm] = True
                    self.vr_to_global_mat[arm] = rot_mat

    def _process_reading(self, arm):
        # for debug use
        if (self._state[arm]["poses"]) is None: return

        rot_mat = np.asarray(self._state[arm]["poses"])
        rot_mat = self.global_to_env_mat @ self.vr_to_global_mat[arm] @ rot_mat
        vr_pos = self.spatial_coeff * rot_mat[:3, 3]
        vr_quat = rmat_to_quat(rot_mat[:3, :3])
        # print(vr_quat)
        vr_gripper = self._state["buttons"]["rightTrig"][0] if arm=='right' else self._state["buttons"]["leftTrig"][0]
        gripper_toggle = self._state[arm]["gripper_toggle"]
        self._state[arm]["gripper_toggle"] = False

        self.vr_state[arm] = {"pos": vr_pos, "quat": vr_quat, "gripper": vr_gripper, "gripper_toggle": gripper_toggle}


    def prase_message(self, message):
        '''
        message format:
        right ; left
        for each side: px py pz rx ry rz rw | a b jsb | tr gr jsx jsy 
        reformat to:
        poses = 
        {
            "r": (4*4), 
            "l": (4*4)
        }

        buttons = 
        {
            "LG": bool, "RG":bool,
            "LJ": bool, "RJ":bool,
            "LJs": bool, "RJs":bool,
            "leftIsGrip": bool, "rightIsGrip": bool,
            "leftTrig": [float], "rightTrig": [float],
            "leftGrip": [float], "rightGrip": [float],        
            "leftJS": [float, float], "rightJS", [float, float]
        }

        G for button A / X in quest3
        J for Y / B
        '''
        # simple format check
        try:
            right, left = message.split(";")
            if right == "" and left == "":
                return {}, {}
        except:
            return {}, {}
        poses, buttons= {}, {}
        right, left = message.split(";")
        for side in ["r", "l"]:
            raw_data = right.strip() if side == "r" else left.strip()
            print(raw_data)
            raw_pose, raw_button, raw_axis = raw_data.split("|")

            # build transformation matrix
            pose = list(map(float, raw_pose.strip().split(" ")))
            position = pose[:3]
            # if (side == "r"):
            #     print([f"{x:.2f}" for x in pose[3:]])
            quat = pose[3:]
            rmat = quat_to_rmat(quat)

            transformation = np.eye(4)
            transformation[:3, :3] = rmat
            transformation[:3, 3] = position

            poses[side] = transformation

            # set button states
            button = [x.strip().lower() == "true" for x in raw_button.strip().split(" ")]
            button_G = 'RG' if side=='r' else 'LG'
            button_J = 'RJ' if side=='r' else 'LJ'
            button_js = 'RJs' if side=='r' else 'LJs'
            buttons[button_G] = button[0]
            buttons[button_J] = button[1]
            buttons[button_js] = button[2]

            # set axis states

            axis = list(map(float, raw_axis.strip().split(" ")))
            trig = 'rightTrig' if side=='r' else 'leftTrig'
            grip = 'rightGrip' if side=='r' else 'leftGrip'
            isGrip = 'rightIsGrip' if side=='r' else 'leftIsGrip'
            js = 'rightJS' if side=='r' else 'leftJS'

            buttons[trig] = [axis[0]]
            buttons[grip] = [axis[1]]
            buttons[isGrip] = axis[1] > 0.5
            buttons[js] = axis[2:]
        return poses, buttons


    def _limit_velocity(self, lin_vel, rot_vel, gripper_vel):
        """Scales down the linear and angular magnitudes of the action"""
        lin_vel_norm = np.linalg.norm(lin_vel)
        rot_vel_norm = np.linalg.norm(rot_vel)
        gripper_vel_norm = np.linalg.norm(gripper_vel)
        if lin_vel_norm > self.max_lin_vel:
            lin_vel = lin_vel * self.max_lin_vel / lin_vel_norm
        if rot_vel_norm > self.max_rot_vel:
            rot_vel = rot_vel * self.max_rot_vel / rot_vel_norm
        if gripper_vel_norm > self.max_gripper_vel:
            gripper_vel = gripper_vel * self.max_gripper_vel / gripper_vel_norm
        return lin_vel, rot_vel, gripper_vel

    def _calculate_action(self, robot_obs: dict[str, np.ndarray], arm: str) -> np.ndarray:
        # Read Sensor #
        if self.update_sensor[arm]:
            self._process_reading(arm)
            self.update_sensor[arm] = False

        # Read Observation
        robot_pos = np.array(robot_obs["cartesian_position"][:3])
        robot_quat = robot_obs["cartesian_position"][3:]

        # Reset Origin On Release #
        if self.reset_origin[arm]:
            self.robot_origin[arm] = {"pos": robot_pos, "quat": robot_quat}
            self.vr_origin[arm] = {"pos": self.vr_state[arm]["pos"], "quat": self.vr_state[arm]["quat"]}
            self.reset_origin[arm] = False

        # Calculate Positional Action #
        robot_pos_offset = robot_pos - self.robot_origin[arm]["pos"]
        target_pos_offset = self.vr_state[arm]["pos"] - self.vr_origin[arm]["pos"]
        pos_action = target_pos_offset * self.pos_action_sign - robot_pos_offset 

        # Calculate Euler Action #
        robot_quat_offset = quat_diff(robot_quat, self.robot_origin[arm]["quat"])
        target_quat_offset = quat_diff(self.vr_state[arm]["quat"], self.vr_origin[arm]["quat"]) 
        # print(self.vr_state[arm]["quat"], self.vr_origin[arm]["quat"])
        quat_action = quat_diff(target_quat_offset * self.quat_action_sign, robot_quat_offset )
        euler_action = quat_to_euler(quat_action) 
        # print(euler_action)
        
        delta_action = np.concatenate((pos_action, euler_action))

        if self.vr_state[arm]["gripper_toggle"]:
            self.target_gripper[arm] = 1 - int(robot_obs["gripper_position"] > 0.5)

        # Prepare Return Values #
        action = np.concatenate([delta_action, [self.target_gripper[arm]]])
        # action = action.clip(-1, 1)
        
        return action

    def get_action(self, obs: TeleopObservation) -> TeleopAction:
        action = self.get_default_action()
        # add button data to action
        buttons = self._state["buttons"]
        action.extra['buttons'] = buttons
        # arm command
        for arm in ['right', 'left']:
            eef_data = obs[arm]
            if eef_data is None:
                continue
            robot_obs = {'cartesian_position': eef_data[:-1], 'gripper_position': eef_data[-1]}
            if self._state[arm]["poses"] is not None:
                action[arm] = self._calculate_action(robot_obs, arm)

        # base command
        base_action = np.zeros(3)
        if 'rightJS' in buttons:
            base_action[0] = buttons['rightJS'][1]
            base_action[1] = -buttons['rightJS'][0]
        if 'leftJS' in buttons:
            base_action[2] = -buttons['leftJS'][0]
        action.base = base_action

        # torso command
        if 'leftJS' in buttons:
            action.torso = 0.01 * buttons['leftJS'][1]

        return action