using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GameManager : MonoBehaviour
{
    public int gameTime;
    public static Action onPauseGame;
    private bool endCountDown = false;

    public List<CharacterScript> prefabs;

    public List<CharacterScript> playersList;

    public CharacterScript localPlayer;

    public CharacterScript otherPlayer;

    public int playerNum;

    //private NewClient client;


    private void OnEnable()
    {
        CharacterScript.onFinishGame += StopCountDown;
    }
    private void OnDisable()
    {

        CharacterScript.onFinishGame -= StopCountDown;
    }

    public void Start()
    {
        playerNum = PhotonNetwork.LocalPlayer.ActorNumber;

        //client = FindObjectOfType<NewClient>();
        //if (client == null)
        //    return;

        //if (client.characterScripts.Count > 0)
        //    return;
        //SpawnPlayer();
        PhotonNetwork.Instantiate(prefabs[playerNum == 1?0:1].name, playerNum==1?new Vector3(7, 0, -19) : new Vector3(17, 0, -19), Quaternion.identity);
        
        StartCoroutine(TimeDown());
    }

    private void Update()
    {
        if (localPlayer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerNum == 1 ? "Player": "Player2") ;
            if (player != null)
            {
                localPlayer = player.GetComponent<CharacterScript>();
            }
        }

        if (otherPlayer == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerNum == 1 ? "Player2" :"Player");
            if (player != null)
            {
                otherPlayer = player.GetComponent<CharacterScript>();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            onPauseGame?.Invoke();
        }
        if (localPlayer == null || otherPlayer == null)
            return;

        if (localPlayer.transform.position.x > otherPlayer.transform.position.x)
        {
            localPlayer.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        }
        else
        {
            localPlayer.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        }

    }

    IEnumerator TimeDown()
    {
        int time = 99;
        while (time > 0 && !endCountDown)
        {
            yield return new WaitForSeconds(1);
            time -= 1;
            UIManager.onUpdateTimer?.Invoke(time);
        }
        if (!endCountDown)
        {
            if (playersList[0].health > playersList[1].health)
            {
                //if (client.clientID == playersList[0].ID)
                //{
                //    CharacterScript.onFinishGame?.Invoke(true);
                //}
                //else
                //{

                //    CharacterScript.onFinishGame?.Invoke(false);
                //}
            }
            else
            {
                //if (client.clientID == playersList[1].ID)
                //{
                //    CharacterScript.onFinishGame?.Invoke(true);
                //}
                //else
                //{

                //    CharacterScript.onFinishGame?.Invoke(false);
                //}

            }
        }

    }

    public void StopCountDown(bool boolean)
    {
        StopCoroutine(TimeDown());
        endCountDown = true;
    }

    public void SpawnPlayer(int i, Vector3 pos)
    {
        CharacterScript character = Instantiate(prefabs[i], pos, Quaternion.identity);
        playersList.Add(character);
        //character.client = client;
        //character.ID = i;
        //client.characterScripts.Add(character);

    }

}
