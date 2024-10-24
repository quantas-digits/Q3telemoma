from component import Component
import zmq
from multiprocessing import Process
from telemoma.utils.general_utils import run_threaded_command

class OculusReaderServer(Component):
    '''
    A class that handle a list of ZMQ sockets to receive data from oculus quest3. 
    Each socket runs on a single process.
    '''
    def __init__(self, host: str, config: dict, mp_share_dict) -> None:
        self.host = host
        self.config = config
        self.mp_share_dict = mp_share_dict
        self.pull_socket_workers = []

    def create_pull_socket(self, host, port):
        context = zmq.Context()
        socket = context.socket(zmq.PULL)
        socket.setsockopt(zmq.CONFLATE, 1)
        socket.bind('tcp://{}:{}'.format(host, port))
        print("created zmq socket: {}:{}".format(host, port))
        return socket
    
    def update_latest_message(self):
        while True:
            datas = []
            for task_name in self.config.keys():
                if task_name not in self.mp_share_dict.keys():
                    self.mp_share_dict[task_name] = ""
                datas.append(self.mp_share_dict[task_name])
            self.mp_share_dict["latest_pose"] = ";".join(datas)
        
    def pull_socket_worker(self, mp_share_dict, name, host, port):
        socket = self.create_pull_socket(host, port)
        print(f"running {name} on {socket}")
        try:
            while True:
                message = socket.recv_string()
                mp_share_dict[name] = message
        finally:
            socket.close()

    def stream(self):
        # launch process for different sockets for listening incoming senser data.
        for task_name, port in self.config.items():
            process = Process(target=self.pull_socket_worker, args=(self.mp_share_dict, task_name, self.host, port))
            process.start() 
            self.pull_socket_workers.append(process)
        
        # launch a thread to update latest recived message.
        run_threaded_command(self.update_latest_message)

    def destory(self):
        for process in self.pull_socket_workers:
            process.terminate()
            process.join()
            print(f"[Server] Process {process.pid} stopped.")