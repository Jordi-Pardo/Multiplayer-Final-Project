using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Scene_Manager : MonoBehaviour
{

    public int sceneIndex;
    public Image black;
    public Animator anim;
    // Start is called before the first frame update

    public void ChangeScene()
    {
        StartCoroutine("Fading");
    }

    IEnumerator Fading()
    {
        anim.SetBool("Fade", true);
        yield return new WaitUntil(() => black.color.a == 1);
        SceneManager.LoadScene(sceneIndex);
    }
}
