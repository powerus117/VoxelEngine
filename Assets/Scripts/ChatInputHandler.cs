using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChatInputHandler : MonoBehaviour {
    private InputField inputField;
    public static bool isTyping;
    public UnityStandardAssets.Characters.FirstPerson.FirstPersonController controller;

    void Start()
    {
        inputField = GetComponent<InputField>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T) && !PauseController.isGamePaused)
        {
            inputField.ActivateInputField();
            inputField.Select();
            isTyping = true;
            controller.SetPlayerLocked(true);
        }
    }

    public void ClearText()
    {
        inputField.text = "";
        EventSystem.current.SetSelectedGameObject(null);
        isTyping = false;
        controller.SetPlayerLocked(false);
    }
}
