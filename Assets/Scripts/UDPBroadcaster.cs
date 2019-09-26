using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using UnityEngine.Networking;

public class UDPBroadcaster : MonoBehaviour {

    public int localPort = 27340;
    public int remotePort = 27341;

    UdpClient sender;
    bool isSending = false;

    public void StartSendingData()
    {
        if (isSending)
            return;
        Debug.Log("Started Broadcasting");
        isSending = true;
        sender = new UdpClient(localPort, AddressFamily.InterNetwork);
        IPEndPoint groupEP = new IPEndPoint(IPAddress.Broadcast, remotePort);
        sender.Connect(groupEP);
        InvokeRepeating("SendData", 0, 2f);
    }

    public void StopSendingData()
    {
        if(isSending)
        {
            Debug.Log("Stopped broadcasting");
            isSending = false;
            CancelInvoke("SendData");
            sender.Close();
        }   
    }

    void SendData()
    {
        string customMessage = LocalIPAddress();
        if(customMessage != "")
            sender.Send(System.Text.Encoding.ASCII.GetBytes(customMessage), customMessage.Length);
        else
        {
            //Error
            Debug.LogError("Didn't find the local IP during UDP broadcasting");
        }
    }

    public string LocalIPAddress()
    {
        IPHostEntry host;
        string localIP = "";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }
        return localIP;
    }
}
