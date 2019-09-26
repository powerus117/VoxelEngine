using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System;
using System.Net;
using System.Text;

public class UDPReceiver : MonoBehaviour {

    UdpClient receiver;
    public int remotePort = 27341;
    IPEndPoint receiveIPGroup;
    public bool isReceiving = false;
    bool hasReceivedMessage = false;
    string ipAddress = "";

    public void StartReceivingIP()
    {
        if (isReceiving)
            return;

        try
        {
            if (receiver == null)
            {
                Debug.Log("Started listening for broadcast");
                isReceiving = true;
                StartCoroutine(ReturnToMainThread());
                receiver = new UdpClient(remotePort);
                receiver.BeginReceive(new AsyncCallback(ReceiveData), null);
            }
        }
        catch (SocketException e)
        {
            Debug.Log(e.Message);
        }
    }

    private void ReceiveData(IAsyncResult result)
    {
        receiveIPGroup = new IPEndPoint(IPAddress.Any, remotePort);
        byte[] received;
        if (receiver != null)
        {
            received = receiver.EndReceive(result, ref receiveIPGroup);
        }
        else
        {
            return;
        }

        string receivedString = Encoding.ASCII.GetString(received);
        ipAddress = receivedString;
        hasReceivedMessage = true;
        isReceiving = false;
        receiver.Close();
        receiver = null;
        Debug.Log("Broadcast received! Stopped listening for broadcast");
    }

    public void StopReceiving()
    {
        if(isReceiving)
        {
            Debug.Log("Stopped listening for broadcast");
            receiver.Close();
            isReceiving = false;
            receiver = null;
            hasReceivedMessage = false;
        }
    }

    IEnumerator ReturnToMainThread()
    {  
        while (isReceiving)
        {
            yield return null;
        }
        if(hasReceivedMessage)
        {
            MainMenuController menu = FindObjectOfType<MainMenuController>();
            if(menu != null)
            {
                menu.AddNewServer(ipAddress);
                StartReceivingIP();
            } 
        }
    }
}
