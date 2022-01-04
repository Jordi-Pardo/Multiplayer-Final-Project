using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static Action<int> onUpdateTimer;
    private List<CharacterScript> players = new List<CharacterScript>();
    public List<Image> healthBars;
    public TextMeshProUGUI gameTime;

    public GameObject winText;
    public GameObject loseText;
    public GameManager gameManager;


    [Header("Pause")]
    public GameObject pause;
    private void Start()
    {
        players = FindObjectsOfType<CharacterScript>().ToList();
        gameManager = FindObjectOfType<GameManager>();
        for (int i = 0; i < players.Count; i++)
        {
            healthBars[i].fillAmount = players[i].health / 100;
        }
    }

    private void OnEnable()
    {
        CharacterScript.onReceiveDamage += UpdateHealth;
        CharacterScript.onFinishGame += ShowFinalMessage;
        onUpdateTimer += UpdateTimer;
        GameManager.onPauseGame += PauseGame;
    }

    private void OnDisable()
    {
        CharacterScript.onReceiveDamage -= UpdateHealth;    
        onUpdateTimer -= UpdateTimer;
        GameManager.onPauseGame -= PauseGame;
        CharacterScript.onFinishGame -= ShowFinalMessage;
    }

    public void UpdateHealth(CharacterScript characterScript)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if(players[i] != characterScript)
            {
                healthBars[i].fillAmount = (float)characterScript.health / 100;
            }
        }
    }
    public void UpdateTimer(int time)
    {
        gameTime.text = time.ToString();
    }

    public void PauseGame()
    {
        pause.SetActive(!pause.activeInHierarchy);
        Time.timeScale = pause.activeInHierarchy ? 0 : 1;
    }

    public void ShowFinalMessage(bool boolean)
    {
        if (gameManager != null)
        {
            gameManager.StopCountDown(true);
        }
        if (boolean)
        {
            winText.SetActive(true);
        }
        else
        {
            loseText.SetActive(true);
        }
    }
}
