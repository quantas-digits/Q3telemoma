using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapActionEndPoint : MonoBehaviour
{   
    [SerializeField]
    public Material onGrabMaterial;

    [SerializeField]
    public Material successConnectMaterial;

    [SerializeField]
    public WsSender webSocketSender;


    private GrabObject grabObject;
    private Material oldMaterial;
    private Renderer ObjectRenderer;
    private OVRInput.Controller grabedHand = OVRInput.Controller.None;

    private bool syncroOn = false;
    private bool previousActionFlag = false;
    void Start()
    {   
        grabObject = GetComponent<GrabObject>();
        ObjectRenderer = GetComponent<Renderer>();
        oldMaterial = ObjectRenderer.material;
        grabObject.GrabbedObjectDelegate += OnGrabbed;
        grabObject.ReleasedObjectDelegate += OnReleased;
    }

    void OnDestroy()
    {
        grabObject.GrabbedObjectDelegate -= OnGrabbed;
        grabObject.ReleasedObjectDelegate -= OnReleased;
    }
    
    void Update()
    {   
        // set send message to socket, if grapped and A(X) pressed start sending information via websocket.
        if (OVRInput.GetDown(OVRInput.Button.One, grabedHand))
        {
            syncroOn = true;
        }
        if (OVRInput.GetUp(OVRInput.Button.One, grabedHand))
        {
            syncroOn = false;
        }

        // 3 prerequisites for syncronization.
        bool isGrabbed = grabedHand != OVRInput.Controller.None;   
        bool webSocketOn = webSocketSender.readyToSend;
        bool actionFlag = isGrabbed && syncroOn && webSocketOn;
        
        if (actionFlag != previousActionFlag)
        {   
            previousActionFlag = actionFlag;
            if (actionFlag)
            {
                if (grabedHand == OVRInput.Controller.LTouch)
                {   
                    webSocketSender.LposeStreaming = true;
                    Debug.Log("[ws] left controller syncronized!");
                    ObjectRenderer.material = successConnectMaterial;
                }
                else if (grabedHand == OVRInput.Controller.RTouch)
                {   
                    webSocketSender.RposeStreaming = true;
                    Debug.Log("[ws] right controller syncronized!");
                    ObjectRenderer.material = successConnectMaterial;
                }
            }
            else
            {
                if (grabedHand == OVRInput.Controller.LTouch)
                {   
                    webSocketSender.LposeStreaming = false;
                    Debug.Log("[ws] left controller disconnected!");
                    ObjectRenderer.material = onGrabMaterial;
                }
                if (grabedHand == OVRInput.Controller.RTouch)
                {   
                    webSocketSender.RposeStreaming = false;
                    Debug.Log("[ws] right controller disconnected!");
                    ObjectRenderer.material = onGrabMaterial;
                }
            }
        }
        
    }
    private void OnGrabbed(OVRInput.Controller grabHand)
    {   
        grabedHand = grabHand;
        oldMaterial = ObjectRenderer.material;
        ObjectRenderer.material = onGrabMaterial;
    }

    private void OnReleased()
    {   
        if (grabedHand == OVRInput.Controller.LTouch)
        {   
            webSocketSender.LposeStreaming = false;
            Debug.Log("[ws] left controller disconnected!");
            ObjectRenderer.material = onGrabMaterial;
        }
        if (grabedHand == OVRInput.Controller.RTouch)
        {   
            webSocketSender.RposeStreaming = false;
            Debug.Log("[ws] right controller disconnected!");
            ObjectRenderer.material = onGrabMaterial;
        }   
        grabedHand = OVRInput.Controller.None;
        ObjectRenderer.material = oldMaterial;

    }
}
