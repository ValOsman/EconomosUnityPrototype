using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour {

    public static Player player;
    public static PlayerManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        player = new Player
        {
            Name = "Player"
        };        
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    

}
