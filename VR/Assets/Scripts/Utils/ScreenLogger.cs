using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScreenLogger : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI logTextBox;

    // Start is called before the first frame update
    private void Start()
    {
        Debug.Log("Started up Logging");
    }

    // Update is called once per frame
    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(String logString, string stackTrace, LogType type)
    {
        logTextBox.text += logString + Environment.NewLine;
    }
}
