using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ClientData : NetworkBehaviour {

    [SyncVar(hook = "ActivateGlider")]
    public bool isGliderActive;

    void Start()
    {
        if(!isLocalPlayer)
        {
            ActivateGlider(isGliderActive);
        }
    }

    [Command]
    public void CmdActivateGlider(bool active)
    {
        Debug.Log("Command glider activate (server)");
        isGliderActive = active;
    }

    public void ActivateGlider(bool active)
    {
        Debug.Log("ActiveGlider on Client");
        GameObject follower = GetComponent<TransformSync>().follower;

        if (follower != null)
        {
            follower.GetComponent<ChildHolder>().glider.SetActive(active);
        }
    }
}
