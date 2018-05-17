using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VillageView : MonoBehaviour {

    [SerializeField]
    private Canvas villageMenu;
    
    public RectTransform villageCard;

    private VillageController villageController;

    // Use this for initialization
    void Start () {
        villageMenu.enabled = false;

        villageController = gameObject.GetComponent<VillageController>();
        Transform card = Instantiate(villageCard, villageMenu.transform, false);

        card.transform.Find("VillageName").GetComponent<Text>().text = villageController.village.Name;
        card.transform.Find("VillageType").GetComponent<Text>().text = villageController.village.VillageType;
        card.transform.Find("Price").GetComponent<Text>().text = villageController.village.ResourceProduced.PriceRange.Max.ToString();
        card.transform.Find("Resource").GetComponent<Text>().text = villageController.village.ResourceProduced.Resource.DisplayName;
        card.transform.Find("BuyButton").GetComponent<Button>().onClick.AddListener(PlayerBuyGood);


    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void PlayerBuyGood()
    {

        ResourceUtil.ResourceType type = villageController.village.ResourceProduced.Resource.Type;

        if (PlayerController.player.HasResource(type) == false)
        {
            PlayerController.player.AddResource(type);
        }

        if (PlayerController.player.ResourceInventory[type].Amount < PlayerController.player.ResourceInventory[type].Max)
        {
            villageController.village.TransferResource(PlayerController.player, type, 1);
            PlayerController.player.TransferCurrency(villageController.village, villageController.village.ResourceProduced.PriceRange.Max);
            EventManager.TriggerEvent("UpdateCurrency");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            villageMenu.enabled = true;
        }

    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            villageMenu.enabled = false;
        }
    }
}
