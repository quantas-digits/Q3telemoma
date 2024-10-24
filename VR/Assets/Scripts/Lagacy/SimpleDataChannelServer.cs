using System.Net.Sockets;
using System.Net;
using UnityEngine;
using WebSocketSharp.Server;

public class SimpleDataChannelServer : MonoBehaviour
{
    private WebSocketServer wssv;
    private string serverIpv4Address;
    private int serverPort = 8080;

    private void Awake()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach(var ip in host.AddressList)
        {   
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                serverIpv4Address = ip.ToString();
                break;
            }
            
        }
        
        Debug.Log($"Starting WebSocket server at ws://{serverIpv4Address}:{serverPort}");
        wssv = new WebSocketServer($"ws://{serverIpv4Address}:{serverPort}");

        wssv.AddWebSocketService<SimpleDataChannelService>($"/{nameof(SimpleDataChannelService)}");
        wssv.Start();

        Debug.Log("WebSocket server started.");
    }
    
}