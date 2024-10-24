from teleop.oculus import OculusPolicy
from telemoma.human_interface.vision import VisionTeleopPolicy
from telemoma.human_interface.keyboard import KeyboardInterface
from telemoma.human_interface.spacemouse import SpaceMouseInterface
from telemoma.human_interface.mobile_phone import MobilePhonePolicy
from telemoma.human_interface.teleop_core import BaseTeleopInterface, TeleopAction, TeleopObservation
from telemoma.utils.general_utils import AttrDict


INTERFACE_MAP = {
    'oculus': OculusPolicy,
    'vision': VisionTeleopPolicy,
    'keyboard': KeyboardInterface,
    'spacemouse': SpaceMouseInterface,
    'mobile_phone': MobilePhonePolicy
}


class TeleopPolicy:
    def __init__(self, config: AttrDict, mp_share_dict) -> None:
        self.config = config
        self.mp_share_dict = mp_share_dict
        self.controllers = {
            'left': config.arm_left_controller,
            'right': config.arm_right_controller,
            'base': config.base_controller,
            'torso': config.torso_controller
        }
        for controller in self.controllers.values():
            if controller is not None:
                assert controller in INTERFACE_MAP, 'Other controllers not implemented.'

        self.interfaces: dict[str, BaseTeleopInterface] = {}
        for part in self.controllers:
            if (self.controllers[part] is not None) and (self.controllers[part] not in self.interfaces):
                self.interfaces[self.controllers[part]] = INTERFACE_MAP[self.controllers[part]](mp_share_dict=self.mp_share_dict,**config.interface_kwargs[self.controllers[part]])

        if ('oculus' not in self.interfaces) and config.get('use_oculus', False):
            self.interfaces['oculus'] = INTERFACE_MAP['oculus']()

    def start(self) -> None:
        for interface in self.interfaces.values():
            if interface is not None:
                interface.start()

    def stop(self) -> None:
        for interface in self.interfaces.values():
            if interface is not None:
                interface.stop()

    def get_default_action(self) -> TeleopAction:
        return TeleopAction()

    def get_action(self, obs: TeleopObservation) -> TeleopAction:
        interface_action = {}
        action = self.get_default_action()
    
        for interface in self.interfaces:
            interface_action[interface] = self.interfaces[interface].get_action(obs)

            for extra_key in interface_action[interface].extra:
                action.extra[extra_key] = interface_action[interface].extra[extra_key]

        for part in self.controllers:
            if (self.controllers[part] is not None) and interface_action[self.controllers[part]][part] is not None:
                action[part] = interface_action[self.controllers[part]][part]
        
        return action
    
    def stop(self):
        for interface in self.interfaces.values():
            if interface is not None:
                interface.stop()