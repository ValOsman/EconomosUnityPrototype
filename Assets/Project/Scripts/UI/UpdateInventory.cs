using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateInventory : MonoBehaviour {

    public RectTransform inventoryUIRow;
    private Dictionary<ResourceUtil.ResourceType, Transform> inventoryRows;

    // Use this for initialization
	void Start () {
        inventoryRows = new Dictionary<ResourceUtil.ResourceType, Transform>();
        EventManager.StartListening("UpdateInventory", UpdateInventoryUI);
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void UpdateInventoryUI()
    {
        Dictionary<ResourceUtil.ResourceType, Player.PlayerResourceItem> playerInventory = PlayerManager.player.ResourceInventory;

        foreach (KeyValuePair<ResourceUtil.ResourceType, Player.PlayerResourceItem> resource in playerInventory)
        {
            if (inventoryRows.ContainsKey(resource.Key))
            {
                UpdateInventoryRow(resource.Key);
            }
            else
            {
                AddInventoryRow(resource.Key);
            }
        }

    }

    private void UpdateInventoryRow(ResourceUtil.ResourceType type)
    {
        inventoryRows[type].transform.Find("Amount").GetComponent<Text>().text = PlayerManager.player.ResourceInventory[type].Amount.ToString() + "/" + PlayerManager.player.ResourceInventory[type].Max.ToString();
    }

    private void AddInventoryRow(ResourceUtil.ResourceType type)
    {
        Transform row = Instantiate(inventoryUIRow, gameObject.transform, false);
        row.transform.Find("ResourceLabel").GetComponent<Text>().text = ResourceUtil.GetResourceByType(type).DisplayName;
        row.transform.name = ResourceUtil.GetResourceByType(type).DisplayName + "Row";
        inventoryRows.Add(type, row);
        UpdateInventoryRow(type);
    }

    private void ReorderList()
    {
        Transform firstChild = gameObject.transform.GetChild(0);
        for (int i = 1; i < gameObject.transform.childCount; i++)
        {
            
        }
    }

}
