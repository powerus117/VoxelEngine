using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkMessageHandler : MonoBehaviour {

    // Message ID's
    public static short chatMsgId = MsgType.Highest + 1;

    // Message classes
    public class ChatMessage : MessageBase
    {
        public string chatMsgText;
    }

}
