using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public List<GameObject> playersUI;
    public Scene_Manager sceneManager;
    private void OnEnable()
    {
        NewClient.onConnectionReceived += UpdatePlayerUI;
    }

    private void OnDisable()
    {
        
        NewClient.onConnectionReceived -= UpdatePlayerUI;
    }

    public void UpdatePlayerUI(int id)
    {
        playersUI[id].SetActive(true);

        if(playersUI[0].activeInHierarchy && playersUI[1].activeInHierarchy)
        {
            sceneManager.ChangeScene();
        }
    }
}
