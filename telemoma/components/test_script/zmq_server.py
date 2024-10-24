import zmq

def start_server():
    # 创建一个 ZeroMQ 上下文
    context = zmq.Context()

    # 创建 PULL 套接字，并绑定到端口 
    socket = context.socket(zmq.PULL)
    socket.bind("tcp://*:50001")

    print("[Server] Waiting for messages...")

    try:
        while True:
            # 接收来自 Unity 客户端的消息
            message = socket.recv_string()
            print(f"[Server] Received: {message}")

    except KeyboardInterrupt:
        print("\n[Server] Server stopped.")

    finally:
        # 关闭套接字和上下文
        socket.close()
        context.term()

if __name__ == "__main__":
    start_server()