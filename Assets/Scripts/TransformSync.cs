using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TransformSync : NetworkBehaviour {

    public GameObject modelPrefab;

    private GameObject syncTrans;
    public GameObject follower;

	// Use this for initialization
	void Start () {
        if(isLocalPlayer)
        {
            syncTrans = GameObject.FindWithTag("Player");
            syncTrans.GetComponent<Glider>().SetNetworkObj(gameObject);
        }
        else
        {
            follower = Instantiate(modelPrefab, transform.position, Quaternion.identity);
            
            Invoke("InitFollower", 0.1f);
        }
	}

    void OnDestroy()
    {
        Destroy(follower);
    }

    private void InitFollower()
    {
        follower.GetComponent<FollowSyncTrans>().Init(transform);
    }
	
	// Update is called once per frame
	void Update () {
        if(isLocalPlayer)
        {
            this.transform.position = syncTrans.transform.position;
            this.transform.rotation = syncTrans.transform.rotation;
        }
    }

}
