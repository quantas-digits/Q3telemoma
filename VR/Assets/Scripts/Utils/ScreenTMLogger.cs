using System;
using UnityEngine;

public class ScreenTMLogger : MonoBehaviour
{
    [SerializeField] private TextMesh logTextBox;
    [SerializeField] private string[] keywords; 

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

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (logTextBox != null)
        {
            if (ContainsKeyword(logString))
            {
                logTextBox.text += logString + Environment.NewLine;
                if (logTextBox.text.Length > 1000)
                {
                    logTextBox.text = logTextBox.text.Substring(logTextBox.text.Length - 1000);
                }
            }
        }
        else
        {
            Debug.LogWarning("logTextBox is not assigned!");
        }
    }

    private bool ContainsKeyword(string logString)
    {
        foreach (string keyword in keywords)
        {
            if (logString.Contains(keyword))
            {
                return true;
            }
        }
        return false;
    }
}
