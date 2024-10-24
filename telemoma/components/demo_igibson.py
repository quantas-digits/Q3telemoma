import numpy as np
from telemoma.robot_interface.igibson import *
from teleop.teleop_policy import TeleopPolicy
from importlib.machinery import SourceFileLoader
from server.OculusReaderServer import OculusReaderServer
from multiprocessing import Process, Pipe, Manager
from utils.read_configs import read_json

COMPATIBLE_ROBOTS = ['tiago', 'fetch']

# python demo_igibson.py --robot tiago --teleop_config configs/only_vr.py

# a method called by Process, make sure the teleop instance context is generated inside a process.
def run_teleop(pipe, mp_share_dict):
    teleop_config = SourceFileLoader('conf', args.teleop_config).load_module().teleop_config
    teleop = TeleopPolicy(teleop_config, mp_share_dict)
    teleop.start()

    while True:
        command, obs = pipe.recv()
        if command == "get_action":
            result = teleop.get_action(obs)
            pipe.send(result)
        elif command == "destory":
            teleop.destory()

def run_server(mp_share_dict):
    server_config = read_json("config/server_config.json")
    server = OculusReaderServer(host=server_config["host"], 
                                config=server_config["server_ports"],
                                mp_share_dict=mp_share_dict)
    server.stream()
    
def main(args):

    # load demo datasets
    from igibson.utils.assets_utils import download_assets, download_demo_data
    download_assets()
    download_demo_data()

    with Manager() as manager:
        # create simulation env
        env = FetchEnv() if args.robot=='fetch' else TiagoEnv()
        obs = env.reset()

        # create share data dictionary
        mp_share_dict = manager.dict({
            "latest_pose": ""
        })

        # deploy server 
        server_process = Process(target=run_server, args=(mp_share_dict,))
        server_process.start()
        # create a proxy object to get action in the running process.
        teleop_parent_conn, teleop_child_conn = Pipe()
        teleop_process = Process(target=run_teleop, args=(teleop_child_conn, mp_share_dict,))
        teleop_process.start()

        while True:
            try:
                # get action from the teleop process via pipe
                teleop_parent_conn.send(("get_action", obs))
                action = teleop_parent_conn.recv() 
               
                buttons = action.extra['buttons'] if 'buttons' in action.extra else {}
            
                if buttons.get('A', False) or buttons.get('B', False):
                    break
                    
                # update env state.
                obs, _, _, _ = env.step(action)

            except KeyboardInterrupt:
                # clean up    
                server_process.terminate()

                teleop_parent_conn.send(("destory", ""))
                action = teleop_parent_conn.recv()
                teleop_process.terminate()
                env.close()

if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument('--robot', type=str, default='tiago', help='Robot to use. Choose between tiago and fetch.')
    parser.add_argument('--teleop_config', type=str, help='Path to the teleop config to use.')
    args = parser.parse_args()

    assert args.robot in COMPATIBLE_ROBOTS, f'Unknown robots. Choose one from: {" ".join(COMPATIBLE_ROBOTS)}' 
    main(args)