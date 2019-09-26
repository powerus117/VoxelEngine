using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatBoxHandler : MonoBehaviour {
    public static int maxLines = 100;

    private Text chatText;
    private List<string> chatLines;

    void Start()
    {
        chatText = GetComponent<Text>();
        chatLines = new List<string>();
    }

    public void SendChatMessage(string text)
    {
        CustomNetworkManager.instance.SendChatMessageToServer(text);
    }

    public void AddLine(string text)
    {
        if(text.Length > 0)
        {
            chatLines.Add(text);
            if(chatLines.Count > maxLines)
            {
                for (int i = 0; i < chatLines.Count - maxLines; i++)
                {
                    chatLines.RemoveAt(i);
                }
            }

            UpdateChatBox();
        }
    }

    private void UpdateChatBox()
    {
        chatText.text = "";

        for (int i = 0; i < chatLines.Count; i++)
        {
            if(i != 0)
            {
                chatText.text += "\n";
            }
            chatText.text += chatLines[i];
        }
    }
}
