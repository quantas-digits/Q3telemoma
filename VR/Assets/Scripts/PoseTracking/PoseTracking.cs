using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using NetMQ;
using NetMQ.Sockets;

using System.Text;
class PoseTracking : MonoBehaviour
{
    //Controllers

    //Network
    private NetworkManager netConfig;
    private PushSocket rightclient;
    private PushSocket leftclient;

    
    private string rightcommunicationAddress;
    private string leftcommunicationAddress;

    private bool connectionEstablished = false;
    private bool leftconnectionEstablished = false;
    private bool rightconnectionEstablished = false;

    private StringBuilder stateInfoL = new StringBuilder();

    private StringBuilder stateInfoR = new StringBuilder();

    public void CreateTCPConnection()
    {   
        try
        {
            rightcommunicationAddress = netConfig.getRightKeypointAddress();
            bool RightAddressAvailable = !string.Equals(rightcommunicationAddress, "tcp://:");
            leftcommunicationAddress = netConfig.getLeftKeypointAddress();
            bool LeftAddressAvailable = !string.Equals(leftcommunicationAddress,  "tcp://:");
            if (RightAddressAvailable)
            {
                // Initiate Push Socket
                rightclient = new PushSocket();
                Debug.Log($"[ws] connecting to {rightcommunicationAddress}");
                rightclient.Connect(rightcommunicationAddress);
                rightconnectionEstablished = true;
            }
            if (LeftAddressAvailable)
            {
                leftclient = new PushSocket();
                Debug.Log($"[ws] connecting to {leftcommunicationAddress}");
                leftclient.Connect(leftcommunicationAddress);
                leftconnectionEstablished = true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ws]Unexpected error occurred during TCP connection setup: {ex.Message}");
        }
    }

    void Start()
    {
        GameObject netConfGameObject = GameObject.Find("NetworkConfigsLoader");
        netConfig = netConfGameObject.GetComponent<NetworkManager>();
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

    
    public void SendRightHandData()
    {
        string dataR = getControllerState(OVRInput.Controller.RTouch).ToString();
        rightclient.SendFrame(dataR);
    }

    public void SendLeftHandData()
    {
        string dataL = getControllerState(OVRInput.Controller.LTouch).ToString();
        leftclient.SendFrame(dataL);

    }

    void Update()
    {
        if (rightconnectionEstablished && leftconnectionEstablished)
        {  
            SendRightHandData();
            SendLeftHandData();          
        }

    }
}