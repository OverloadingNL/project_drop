using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public float timeToLoadScene = 2;
    public string sceneToLoad;

    void Start()
    {
        Invoke("GoToScene", timeToLoadScene);
    }

    void GoToScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }

}
