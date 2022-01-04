using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int gameTime;
    public static Action onPauseGame;
    private bool endCountDown = false;

    public List<CharacterScript> prefabs;

    public List<CharacterScript> playersList;

    private NewClient client;


    private void OnEnable()
    {
        CharacterScript.onFinishGame += StopCountDown;
    }
    private void OnDisable()
    {
        
        CharacterScript.onFinishGame -= StopCountDown;
    }

    private void Start()
    {
       
        StartCoroutine(TimeDown());
        client = FindObjectOfType<NewClient>();
        if (client == null)
            return;

        if (client.characterScripts.Count > 0)
            return;
        for (int i = 0; i < client.positionsDic.Count; i++)
        {
            SpawnPlayer(i,client.positionsDic[i]);

        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            onPauseGame?.Invoke();
        }
        if (playersList.Count <= 0)
            return;

        if(playersList[0].transform.position.x > playersList[1].transform.position.x)
        {
            playersList[0].transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            playersList[1].transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        }
        else
        {
            playersList[0].transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            playersList[1].transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
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
                if (client.clientID == playersList[0].ID)
                {
                    CharacterScript.onFinishGame?.Invoke(true);
                }
                else
                {

                    CharacterScript.onFinishGame?.Invoke(false);
                }
            }
            else
            {
                if (client.clientID == playersList[1].ID)
                {
                    CharacterScript.onFinishGame?.Invoke(true);
                }
                else
                {

                    CharacterScript.onFinishGame?.Invoke(false);
                }

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
        character.client = client;
        character.ID = i;
        client.characterScripts.Add(character);

    }

}
