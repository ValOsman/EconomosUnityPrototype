using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{

    [SerializeField]
    private float speed;

    [SerializeField]
    private Animator animator;

    private static int distanceTraveled;

    float moveHorizontal;
    float moveVertical;
    float distanceMovedX;
    float distanceMovedY;
    bool playerMoving;
    Vector2 lastDirection;
    Vector2 startPoint;

    public bool PlayerMoving
    {
        get { return playerMoving; }
    }

    public int DistanceTraveled
    {
        get { return distanceTraveled; }
    }

    private void Start()
    {

    }

    private void Update()
    {
        moveHorizontal = Input.GetAxisRaw("Horizontal");
        moveVertical = Input.GetAxisRaw("Vertical");
    }

    private void FixedUpdate()
    {
        playerMoving = false;
        distanceMovedX = 0;
        distanceMovedY = 0;
        startPoint = transform.position;

        if (moveHorizontal != 0)
        {            
            transform.Translate(moveHorizontal * speed, 0, 0);
            distanceMovedX = transform.position.x - startPoint.x;
            playerMoving = true;
            lastDirection = new Vector2(moveHorizontal, 0);
        }

        if (moveVertical != 0)
        {            
            transform.Translate(0, moveVertical * speed, 0);
            distanceMovedY = transform.position.y - startPoint.y;
            playerMoving = true;
            lastDirection = new Vector2(0, moveVertical);
        }

        distanceTraveled += (int)Math.Ceiling(Math.Abs(distanceMovedX) + Math.Abs(distanceMovedY));
        

        animator.SetBool("PlayerMoving", playerMoving);
        animator.SetFloat("DirectionX", moveHorizontal);
        animator.SetFloat("DirectionY", moveVertical);
        animator.SetFloat("LastDirectionX", lastDirection.x);
        animator.SetFloat("LastDirectionY", lastDirection.y);

    }
}
