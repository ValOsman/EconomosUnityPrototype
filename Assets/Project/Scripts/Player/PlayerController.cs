using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    public static Player player;
    public static PlayerController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        player = new Player
        {
            Name = "Nina"
        };
    }

    // Use this for initialization
    void Start () {
        EventManager.TriggerEvent("UpdateCurrency");
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    

}
