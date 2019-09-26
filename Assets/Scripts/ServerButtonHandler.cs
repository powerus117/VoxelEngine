using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ServerButtonHandler : MonoBehaviour {

    public string ipAdress;
    [SerializeField]
    private Text buttonText;

    void Awake()
    {
        ipAdress = "";
    }

    public void SetIPAddress(string ip)
    {
        ipAdress = ip;
        buttonText.text = "IP: " + ip;
    }

    public void JoinThisServer()
    {
        CustomNetworkManager.instance.StartAsClient(ipAdress);
    }
}
