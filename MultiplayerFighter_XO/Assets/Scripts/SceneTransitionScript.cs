using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionScript : MonoBehaviour
{
    // Start is called before the first frame update
    public Animator animator;
    private int levelToLoad;

    // Update is called once per frame
    void Update()
    {
        
    }

    public void FadeToLevel (int scene)
    {
        levelToLoad = scene;
        animator.SetTrigger("FadeOut");
    }

    public void OnFadeCompleted()
    {
        SceneManager.LoadScene(levelToLoad);
    }
}
