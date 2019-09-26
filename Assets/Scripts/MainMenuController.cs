using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public Button stopConnectingButton;
    private List<ServerButtonHandler> foundServers;
    public GameObject serverListObj;
    public GameObject serverButtonPrefab;

    void Awake()
    {
        foundServers = new List<ServerButtonHandler>();
    }

    public void StartListeningForBroadcast()
    {
        CustomNetworkManager.instance.StartListeningForIp();
    }

    public void AddNewServer(string ip)
    {
        foreach(ServerButtonHandler SButton in foundServers)
        {
            if (SButton.ipAdress == ip)
            {
                Debug.Log("Same IP received");
                return;
            }
        }
        Debug.Log("New IP received");
        GameObject aServerButton = Instantiate(serverButtonPrefab, serverListObj.transform);
        foundServers.Add(aServerButton.GetComponent<ServerButtonHandler>());
        aServerButton.GetComponent<ServerButtonHandler>().SetIPAddress(ip);
    }

    public void StartHosting()
    {
        CustomNetworkManager.instance.StartHosting();
    }

    public void StartClient()
    {
        CustomNetworkManager.instance.StartAsClient();
        stopConnectingButton.gameObject.SetActive(true);
    }

    public void SetNewIpAdress(string ip)
    {
        CustomNetworkManager.instance.networkAddress = ip;
    }

    public void StopHosting()
    {
        CustomNetworkManager.instance.StopHosting();
        stopConnectingButton.gameObject.SetActive(false);
    }

    public void SetPlayerName(string name)
    {
        CustomNetworkManager.playerName = name;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
