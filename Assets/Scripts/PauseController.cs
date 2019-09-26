using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;

public class PauseController : MonoBehaviour {

    public CanvasGroup pauseMenu;
    public UnityStandardAssets.Characters.FirstPerson.FirstPersonController controller;
    public static bool isGamePaused;

    void Start()
    {
        controller.SetPlayerLocked(false);
        isGamePaused = false;
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseGame();
        }
    }

    public void TogglePauseGame()
    {
        isGamePaused = !isGamePaused;

        //pauseMenu.SetActive(isGamePaused);
        float endVal = isGamePaused ? 1 : 0;
        DOTween.To(x => pauseMenu.alpha = x, pauseMenu.alpha, endVal, 0.1f);

        pauseMenu.blocksRaycasts = isGamePaused;
        pauseMenu.interactable = isGamePaused;

        controller.SetPlayerLocked(isGamePaused);
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void LeaveGame()
    {
        CustomNetworkManager.instance.StopHosting();

    }
}
