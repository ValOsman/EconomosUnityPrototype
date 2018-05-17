using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VillageController : MonoBehaviour {

    public Village village;

    private VillageView menu;

    [SerializeField]
    private ResourceUtil.ResourceType villageResource;

    // Use this for initialization
    void Awake () {

        village = new Village("Athenry", 100, ResourceUtil.GetResourceByType(villageResource));

        int fuck = 1 + 1;

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
