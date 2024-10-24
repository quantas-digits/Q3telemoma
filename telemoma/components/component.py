from abc import ABC, abstractmethod

class Component(ABC):
    @abstractmethod
    def stream(self):
        '''Run a forever loop to update or receive data, or something else.'''
        raise NotImplementedError()
    
    def destory(self):
        '''Clean up when the component is destory.'''
        raise NotImplementedError()
    
    def notify_component_start(self, component_name):
        print("***************************************************************")
        print("     Starting {} component".format(component_name))
        print("***************************************************************")