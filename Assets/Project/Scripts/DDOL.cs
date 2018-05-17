using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DDOL : MonoBehaviour {

    public void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Debug.Log("Preload");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Continent-Test");
    }
}
