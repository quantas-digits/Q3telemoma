using System;
using System.Net.Sockets;
using UnityEngine;
using WebSocketSharp;
using UnityEngine.UI;
using System.Net;
using System.Text;
using System.Collections;

public class WsSender : MonoBehaviour
{
    public InputField InputField;
    
    public bool RposeStreaming = false;
    public bool LposeStreaming = false;

    public bool readyToSend;
    private WebSocket ws = null;
    private string serverIp;
    private string port;
    private float sendInterval = 0.02f;
    private string serverIpv4Address;
    private Coroutine sendDataCoroutine;


    private UdpClient udpClient = null;
    private IPEndPoint serverEndPoint = null;

    private TcpClient tcpClient = null;
    private NetworkStream tcpStream = null;

    private StringBuilder controllerState = new StringBuilder();
    private StringBuilder stateInfoL = new StringBuilder();
    private StringBuilder stateInfoR = new StringBuilder();

    private const int SendBufferSize = 256 * 1024; 
    private const int ReceiveBufferSize = 64 * 1024; // 64 KB
    void Start()
    {
        // sendDataCoroutine = StartCoroutine(SendDataContinuouslyAsync()); 
    }

    void Update()
    {   
        bool ws_ready = (ws != null) && ws.IsAlive;
        bool udp_ready = udpClient != null;
        bool tcp_ready = tcpClient != null && tcpClient.Connected;
        readyToSend = tcp_ready || udp_ready || ws_ready; 
        updateControllerState();
    }

    private IEnumerator SendDataContinuouslyAsync()
    {
        while (true)
        {   
            // Debug.Log($"[ws] {readyToSend}");
            if(readyToSend)
            {   
                if ((ws != null) && ws.IsAlive)
                {
                    ws.SendAsync(controllerState.ToString(), null);
                }
                else if (udpClient != null) 
                {
                    udp_send(controllerState.ToString());
                }
                else if (tcpClient != null && tcpClient.Connected)
                {   
                    tcp_send(controllerState.ToString());
                }
                else
                {   
                    Debug.Log("[ws]Unknown connection");
                }
            }
            yield return new WaitForSeconds(sendInterval);
        }
    }

    private void StopSendingThread()
    {
        readyToSend = false;

        if (sendDataCoroutine != null)
        {
            StopCoroutine(sendDataCoroutine);
            sendDataCoroutine = null;
        }
        tcpClient = null;
        tcpStream = null;
        udpClient = null;
        serverEndPoint = null;
        ws = null;
    }

    private void OnDestroy()
    {
        StopSendingThread();  
    }

    public void disconnect()
    {   
        readyToSend = false;
        Debug.Log("[ws] Web disconnected.");
        tcpClient = null;
        tcpStream = null;
        udpClient = null;
        serverEndPoint = null;
        ws = null;
    }

    public void udp_connect()
    {   
        if (udpClient != null)
        {
            Debug.Log("[ws] UDP already connected!");
            return;
        }

        try
        {
            string sanitizedInput = InputField.text.Replace(" ", "");
            string[] url = sanitizedInput.Split(':');

            if (url.Length == 2 && !string.IsNullOrEmpty(url[0]) && !string.IsNullOrEmpty(url[1]))
            {
                serverIp = url[0];
                port = url[1];

                Debug.Log($"[ws]Connecting to udp {serverIp}:{port} ......");
                udpClient = new UdpClient();
                udpClient.Client.SendBufferSize = SendBufferSize;
                udpClient.Client.ReceiveBufferSize = ReceiveBufferSize;
                serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), int.Parse(port));
                // udp_send("hello server!");
                Debug.Log($"[ws]UDP Connected! Start sending controller data...");
                sendDataCoroutine = StartCoroutine(SendDataContinuouslyAsync()); 
                
            }

        }
        catch (Exception e)
        {
            Debug.Log("[ws]UDP connection failed: " + e.Message);
        }
        

    }
    void OnReceive(IAsyncResult result)
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] receivedData = udpClient.EndReceive(result, ref remoteEndPoint);
        string receivedMessage = Encoding.UTF8.GetString(receivedData);
        Debug.Log("udp received from server: " + receivedMessage);
    }

    void udp_send(string s)
    {
        byte[] data = Encoding.UTF8.GetBytes(s);
        udpClient.Send(data, data.Length, serverEndPoint);
        // udpClient.BeginReceive(new AsyncCallback(OnReceive), null);
    }

    public void ws_connect()
    {   
        if ((ws != null) && ws.IsAlive)
        {
            Debug.Log("[ws] websocket already connected!");
            return;
        }
       
        try
        {
            string sanitizedInput = InputField.text.Replace(" ", "");
            string[] url = sanitizedInput.Split(':');

            if (url.Length == 2 && !string.IsNullOrEmpty(url[0]) && !string.IsNullOrEmpty(url[1]))
            {
                serverIp = url[0];
                port = url[1];

                Debug.Log($"[ws]Connecting to ws://{serverIp}:{port} ......");
                ws = new WebSocket($"ws://{serverIp}:{port}");

                ws.OnOpen += (sender, e) =>
                {
                    Debug.Log("[ws]WebSocket connected!");
                    sendDataCoroutine = StartCoroutine(SendDataContinuouslyAsync());
                };

                ws.OnMessage += (sender, e) =>
                {
                    Debug.Log("[ws]Message from server: " + e.Data);
                };

                ws.OnError += (sender, e) =>
                {
                    Debug.LogError("[ws]WebSocket Error: " + e.Message);
                    StopSendingThread(); 
                };

                ws.OnClose += (sender, e) =>
                {
                    Debug.Log("[ws]WebSocket closed: " + e.Reason);
                    StopSendingThread();  
                };

                ws.Connect();
            }
            else
            {
                Debug.Log("[ws]Invalid input format. Please use the format 'IP:PORT'.");
            }
        }
        catch (Exception e)
        {
            Debug.Log("[ws]WebSocket connection failed: " + e.Message);
        }
        
    }

    public void tcp_connect()
    {   
        if (tcpClient != null)
        {
            Debug.Log("[ws] tcp server already connected!");
            return;
        }

        try
        {  
            string sanitizedInput = InputField.text.Replace(" ", "");
            string[] url = sanitizedInput.Split(':'); 
            if (url.Length == 2 && !string.IsNullOrEmpty(url[0]) && !string.IsNullOrEmpty(url[1]))
            {
                // serverEndPoint = new IPEndPoint(IPAddress.Parse(url[0]), int.Parse(url[1]));
                tcpClient = new TcpClient(url[0], int.Parse(url[1]));
                // tcpClient.Connect(IPAddress.Parse(url[0]), int.Parse(url[1]));
                tcpStream = tcpClient.GetStream();
                Debug.Log("[ws]TCP Client connected.");
                sendDataCoroutine = StartCoroutine(SendDataContinuouslyAsync());
            }
            
        }
        catch (SocketException e)
        {
            Debug.LogError($"[ws]SocketException: {e.Message}");
            return;
        }
        
    }

    private void tcp_send(string s)
    {
        byte[] data = Encoding.UTF8.GetBytes(s);  
        tcpStream.Write(data, 0, data.Length);  
        // Debug.Log($"Sent: {s}");
    }

    private StringBuilder getControllerState(OVRInput.Controller controllerType)
    {   
        StringBuilder stateInfo;
        if (controllerType == OVRInput.Controller.LTouch)
        {
            stateInfo = stateInfoL;
        }
        else
        {
            stateInfo = stateInfoR;
        }
        stateInfo.Clear();  
        Vector3 position = OVRInput.GetLocalControllerPosition(controllerType);
        Quaternion rotation = OVRInput.GetLocalControllerRotation(controllerType);
        stateInfo.AppendFormat("{0} {1} {2} {3} {4} {5} {6} | ", 
                            position.x, position.y, position.z, 
                            rotation.x, rotation.y, rotation.z, rotation.w);

        bool AButton = OVRInput.Get(OVRInput.Button.One, controllerType);
        bool BButton = OVRInput.Get(OVRInput.Button.Two, controllerType);
        bool ThumbstickButton = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, controllerType);
        stateInfo.AppendFormat("{0} {1} {2} | ", AButton, BButton, ThumbstickButton);

        float triggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controllerType);
        float gripValue = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controllerType);
        Vector2 leftThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controllerType);
        stateInfo.AppendFormat("{0} {1} {2} {3}", triggerValue, gripValue, leftThumbstick.x, leftThumbstick.y);

        return stateInfo;
    }

    private void updateControllerState()
    {   controllerState.Clear();
        StringBuilder  Rstate = getControllerState(OVRInput.Controller.RTouch);
        StringBuilder  Lstate = getControllerState(OVRInput.Controller.LTouch);
        controllerState.Append(Rstate).Append(";").Append(Lstate);
    }

    public string GetLocalIPAddress()
    {  
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                serverIpv4Address = ip.ToString();
                break;
            }
        }

        return serverIpv4Address;
    }
}
