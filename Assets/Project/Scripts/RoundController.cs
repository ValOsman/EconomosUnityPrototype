using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RoundController : MonoBehaviour {

    private const int STEP_UPDATE_INTERVAL = 100;
    private long previousDistanceTraveled;
    private long currentDistanceTraveled;

    [SerializeField]
    private PlayerMovement playerMovement;

    private UnityAction roundUpdateEvent;

    public void Awake()
    {
        previousDistanceTraveled = 0;
        roundUpdateEvent = new UnityAction(NextRound);
        EventManager.StartListening("UpdateRound", roundUpdateEvent);
    }

    private void FixedUpdate()
    {
        currentDistanceTraveled = playerMovement.DistanceTraveled;
        if (playerMovement.PlayerMoving == true && (currentDistanceTraveled - previousDistanceTraveled) >= STEP_UPDATE_INTERVAL && playerMovement.DistanceTraveled > 0)
        {
            Debug.Log("DistanceTraveled = " + playerMovement.DistanceTraveled);
            EventManager.TriggerEvent("UpdateRound");
            previousDistanceTraveled = currentDistanceTraveled;
        }
        
    }

    private void NextRound()
    {
        TimeUtil.IncrementRound();
    }
}
