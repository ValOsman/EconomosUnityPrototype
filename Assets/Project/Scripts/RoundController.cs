using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RoundController : MonoBehaviour {

    private const int STEP_UPDATE_INTERVAL = 80;

    [SerializeField]
    private PlayerMovement playerMovement;

    private UnityAction roundUpdateEvent;

    public void Awake()
    {
        roundUpdateEvent = new UnityAction(NextRound);
        EventManager.StartListening("UpdateRound", roundUpdateEvent);
    }

    private void FixedUpdate()
    {
        if (playerMovement.PlayerMoving == true && playerMovement.DistanceTraveled % STEP_UPDATE_INTERVAL == 0 && playerMovement.DistanceTraveled > 0)
        {
            Debug.Log("DistanceTraveled = " + playerMovement.DistanceTraveled);
            EventManager.TriggerEvent("UpdateRound");
        }
    }

    private void NextRound()
    {
        TimeUtil.IncrementRound();
    }
}
