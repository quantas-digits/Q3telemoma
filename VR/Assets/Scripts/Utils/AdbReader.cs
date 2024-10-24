using System;
using UnityEngine;
using System.Text;
using System.IO;
using UnityEngine.Android;

public class AdbReader : MonoBehaviour
{   

    private StringBuilder controllerState = new StringBuilder();
    private StringBuilder stateInfoL = new StringBuilder();
    private StringBuilder stateInfoR = new StringBuilder();

    // private StreamWriter writer;

    private int maxLines = 100;
    private int appended_lines = 0;
    private Coroutine poseCoroutine;
    private Coroutine writeCoroutine;

    private string filePath;
    // Start is called before the first frame update
    void Start()
    {
        // writer = new StreamWriter("/sdcard/pose_data.txt", append: true)
        // {
        //     AutoFlush = true 
        // };

        // poseCoroutine = StartCoroutine(updateControllerState());
        // writeCoroutine = StartCoroutine(readControllerState());
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
        filePath = Application.persistentDataPath + "pose_data.txt";
    }

    // Update is called once per frame
    void Update()
    {
        updateControllerState();
        readControllerState();
        clearFileIfTooLong();
    }

    void OnDestroy()
    {   
        // File.Delete();
        // writer.Close();
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
        // yield return new WaitForSeconds(0.02f);
    }

    private void clearFileIfTooLong()
    {
        
        if (appended_lines > maxLines)
        {   
            string[] lines = File.ReadAllLines(filePath);
            File.WriteAllText(filePath, lines[lines.Length-1] + "\n");
            appended_lines = 1;
        }
    }
    private void readControllerState()
    {   
        string state = controllerState.ToString();
        // Debug.Log($"[ws] Oculus state: {state}");
        try{
            File.AppendAllText(filePath, state + "\n");
            appended_lines += 1;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ws]Failed to write to file: {ex.Message}");
        }
        // writer.WriteLine(state);
        // yield return new WaitForSeconds(0.02f);
    }
}
