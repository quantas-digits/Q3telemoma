using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardFocus : MonoBehaviour
{   
    [SerializeField]
    public OVRVirtualKeyboardInputFieldTextHandler handler;
    [SerializeField]
    public InputField inputField;

    private bool isFocused = false;
    
    void Start()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(OnInputFieldFocused);
            inputField.onEndEdit.AddListener(OnInputFieldDeselected);
        }
    }

    private void OnInputFieldFocused(string text)
    {
        if (!isFocused)
        {
            isFocused = true;
            if (handler != null)
            {
                handler.InputField = inputField;
            }
        }
    }

    private void OnInputFieldDeselected(string text)
    {
        isFocused = false;
    }

    void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(OnInputFieldFocused);
            inputField.onEndEdit.RemoveListener(OnInputFieldDeselected);
        }
    }
}
