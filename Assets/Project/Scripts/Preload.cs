using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Preload : MonoBehaviour
{
    void Awake()
    {
        GameObject check = GameObject.Find("_app");
        if (check == null)
        { UnityEngine.SceneManagement.SceneManager.LoadScene("_preload"); }
    }
}
