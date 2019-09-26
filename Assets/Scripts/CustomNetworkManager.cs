using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CustomNetworkManager : NetworkManager {

    private static CustomNetworkManager _instance;
    public static CustomNetworkManager instance
    {
        get
        {
            return _instance;
        }
    }

    public int playerCount;
    public static string playerName = "NoName";
    
    void Awake()
    {
        if (FindObjectsOfType<CustomNetworkManager>().Length > 1)
        {
            DestroyImmediate(gameObject);
            return;
        }

        if(_instance == null || _instance != this)
        {
            DestroyImmediate(_instance);
        }

        _instance = this;
    }

	public override void OnServerConnect(NetworkConnection conn)
    {
        base.OnServerConnect(conn);
        playerCount++;
        Debug.Log("Player with IP: " + conn.address + " connected, total players: " + playerCount);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
        playerCount--;
        SendChatMessageToClients("Player with IP: " + conn.address + " disconnected, total players: " + playerCount);
    }

    public override void OnStartClient(NetworkClient client)
    {
        base.OnStartClient(client);

        // Register functions for messages received on the client
        client.RegisterHandler(NetworkMessageHandler.chatMsgId, OnChatMessageReceivedClient);
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);

        SendCleanChatMessageToServer(playerName + " Has Connected!");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Register functions for messages received on the server
        NetworkServer.RegisterHandler(NetworkMessageHandler.chatMsgId, OnChatMessageReceivedServer);
    }

    // Send a message to all clients
    public void SendChatMessageToClients(string msgText)
    {
        NetworkMessageHandler.ChatMessage msg = new NetworkMessageHandler.ChatMessage();
        msg.chatMsgText = msgText;
        NetworkServer.SendToAll(NetworkMessageHandler.chatMsgId, msg);
    }

    private void SendCleanChatMessageToServer(string msgText)
    {
        NetworkMessageHandler.ChatMessage msg = new NetworkMessageHandler.ChatMessage();
        msg.chatMsgText =  msgText;
        client.Send(NetworkMessageHandler.chatMsgId, msg);
    }

    public void SendChatMessageToServer(string msgText)
    {
        SendCleanChatMessageToServer(playerName + ": " + msgText);
    }

    // This handles the message received on the server
    public void OnChatMessageReceivedServer(NetworkMessage netMsg)
    {
        NetworkMessageHandler.ChatMessage msg = netMsg.ReadMessage<NetworkMessageHandler.ChatMessage>();
        
        Debug.Log("Server msg received: " + msg.chatMsgText);

        SendChatMessageToClients(msg.chatMsgText);
    }

    // This handles the message received on the client
    public void OnChatMessageReceivedClient(NetworkMessage netMsg)
    {
        NetworkMessageHandler.ChatMessage msg = netMsg.ReadMessage<NetworkMessageHandler.ChatMessage>();
        Debug.Log(msg.chatMsgText);
        
        ChatBoxHandler chat = FindObjectOfType<ChatBoxHandler>();
        if(chat != null)
        {
            chat.AddLine(msg.chatMsgText);
        }
    }

    public void StartAsClient(string ipAdress)
    {
        networkAddress = ipAdress;
        StartAsClient();
    }

    public void StartAsClient()
    {
        GetComponent<UDPReceiver>().StopReceiving();
        StartClient();
    }

    public void StartHosting()
    {
        GetComponent<UDPReceiver>().StopReceiving();
        CustomNetworkManager.instance.StartHost();
        GetComponent<UDPBroadcaster>().StartSendingData();
    }

    public void StopHosting()
    {
        GetComponent<UDPBroadcaster>().StopSendingData();
        CustomNetworkManager.instance.StopHost();
    }

    public void StartListeningForIp()
    {
        GetComponent<UDPReceiver>().StartReceivingIP();
    }
}
