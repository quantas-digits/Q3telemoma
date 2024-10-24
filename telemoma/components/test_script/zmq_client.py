import zmq
import time

def start_client():
    # 创建一个 ZeroMQ 上下文
    context = zmq.Context()

    # 创建 PUSH 套接字，并连接到服务器的端口
    socket = context.socket(zmq.PUSH)
    socket.connect("tcp://localhost:50000")  # 连接到服务器地址

    print("[Client] Connected to server.")

    try:
        for i in range(10):
            # 发送消息给服务器
            message = f"Hello from client {i}"
            socket.send_string(message)
            print(f"[Client] Sent: {message}")

            # 模拟发送间隔
            time.sleep(1)

    except KeyboardInterrupt:
        print("\n[Client] Client stopped.")

    finally:
        # 关闭套接字和上下文
        socket.close()
        context.term()

if __name__ == "__main__":
    start_client()
