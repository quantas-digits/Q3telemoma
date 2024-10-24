using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerPositionInfo : MonoBehaviour
{   
    public TextMesh textMesh;
    public OVRInput.Controller controllerR = OVRInput.Controller.RTouch;
    public OVRInput.Controller controllerL = OVRInput.Controller.LTouch;

    // Update is called once per frame
    void Update()
    {
        Vector3 controllerPositionR = OVRInput.GetLocalControllerPosition(controllerR);
        Quaternion controllerRotationR = OVRInput.GetLocalControllerRotation(controllerR);

        Vector3 controllerPositionL = OVRInput.GetLocalControllerPosition(controllerL);
        Quaternion controllerRotationL = OVRInput.GetLocalControllerRotation(controllerL);

        float triggerR = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controllerR);
        float triggerL = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controllerL);

        string positionTextR = $"PositionR: X: {controllerPositionR.x:F2}, Y: {controllerPositionR.y:F2}, Z: {controllerPositionR.z:F2}";
        string rotationTextR = $"RotationR: X: {controllerRotationR.eulerAngles.x:F2}, Y: {controllerRotationR.eulerAngles.y:F2}, Z: {controllerRotationR.eulerAngles.z:F2}";

        string positionTextL = $"PositionL: X: {controllerPositionL.x:F2}, Y: {controllerPositionL.y:F2}, Z: {controllerPositionL.z:F2}";
        string rotationTextL = $"RotationL: X: {controllerRotationL.eulerAngles.x:F2}, Y: {controllerRotationL.eulerAngles.y:F2}, Z: {controllerRotationL.eulerAngles.z:F2}";

        string triggerTextR = $"TriggerR: {triggerR}";
        string triggerTextL = $"TriggerL: {triggerL}";
        
        textMesh.text = positionTextR + "\n" + rotationTextR + "\n" + positionTextL + "\n" + rotationTextL + "\n" + triggerTextR + triggerTextL;

    }
}
