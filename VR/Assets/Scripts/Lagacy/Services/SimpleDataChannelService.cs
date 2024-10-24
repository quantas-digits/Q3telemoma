using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class SimpleDataChannelService : WebSocketBehavior
{
    protected override void OnOpen()
    {
        Debug.Log("SERVER SimpleDataChannelService started!");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        Debug.Log(ID + " - DataChannel SERVER got message" + e.Data);

        // forward messages to all other clients
        foreach (var id in Sessions.ActiveIDs)
        {
            if (id != ID)
            {
                Sessions.SendTo(e.Data, id);
            }
        }
    }

     protected override void OnClose(CloseEventArgs e)
    {
        Debug.Log("WebSocket connection closed.");
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Debug.LogError("WebSocket error: " + e.Message);
    }
}
