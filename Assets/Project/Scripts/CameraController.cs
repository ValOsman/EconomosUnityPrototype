using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class CameraController : MonoBehaviour {

    public GameObject player;

    private Vector3 offset;

    // Use this for initialization
    private void Start ()
    {
        offset = transform.position - player.transform.position;
	}
	
	// Update is called once per frame
	private void Update ()
    {
        
	}

    // Called once per frame AFTER everything has been called in Update
    private void LateUpdate()
    {
        transform.position = player.transform.position + offset;
    }
}
