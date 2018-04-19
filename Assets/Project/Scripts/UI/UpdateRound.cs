using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class UpdateRound : MonoBehaviour {

    // Update every X number of meters moved
    private const int STEP_UPDATE_INTERVAL = 80;

    private Text roundText;

    private UnityAction roundUpdateListener;

	// Use this for initialization
	void Start ()
    {
        roundUpdateListener = new UnityAction(UpdateDate);
        EventManager.StartListening("UpdateRound", roundUpdateListener);
        roundText = gameObject.GetComponent<Text>();
        roundText.text = PrintDate();
	}
    
    private void UpdateDate()
    {
        roundText.text = PrintDate();
    }

    private string PrintDate()
    {
        return String.Format("{0} of {1}, {2}", TimeUtil.GetDay(), TimeUtil.GetMonth(), TimeUtil.Year);
    }
}
