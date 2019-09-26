using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Glider : MonoBehaviour {
    
    [HideInInspector]
    public GameObject networkObj;

    private UnityStandardAssets.Characters.FirstPerson.FirstPersonController controller;

    private bool isGliderActive;
    private float startGrav;

    void Start () {
        controller = GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>();
        startGrav = controller.m_GravityMultiplier;
	}

    public void SetNetworkObj(GameObject obj)
    {
        this.networkObj = obj;
    }
	
	void Update () {
		if(Input.GetKeyDown(KeyCode.G) && !ChatInputHandler.isTyping)
        {
            ToggleGlider();
        }

        if(isGliderActive)
        {
            if (controller.GetMovement().y <= 0)
            {
                controller.m_GravityMultiplier = 0.3f;
            }
            else
            {
                controller.m_GravityMultiplier = startGrav;
            }
        }
	}

    private void ToggleGlider()
    {
        isGliderActive = !isGliderActive;

        if(isGliderActive)
        {
            // Glider became active
            controller.speedMultiplier = 3f;
        }
        else
        {
            // Glider became inactive
            controller.speedMultiplier = 1f;
            controller.m_GravityMultiplier = startGrav;
        }

        networkObj.GetComponent<ClientData>().CmdActivateGlider(isGliderActive);
    }
}
