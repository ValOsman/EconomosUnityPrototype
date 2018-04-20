using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShowTownMenu : MonoBehaviour {

    [SerializeField]
    private Canvas townMenu;
    private Text priceMenu;
    private Transform priceMenuGrid;
    private Dictionary<Transform, ResourceUtil.ResourceType> rows;
    public RectTransform priceMenuRow;

    private TownManager townManager;

    private void Start()
    {
        townMenu.enabled = false;
        townManager = gameObject.GetComponent<TownManager>();
        rows = new Dictionary<Transform, ResourceUtil.ResourceType>();
        priceMenuGrid = townMenu.transform.Find("PriceGrid");

        foreach (KeyValuePair<ResourceUtil.ResourceType, Market> market in townManager.town.Markets)
        {
            string resourceName = market.Value.Resource.DisplayName;
            Transform row = Instantiate(priceMenuRow, priceMenuGrid.transform, false);
            row.name = resourceName + "Row";
            row.transform.Find("ResourceLabel").GetComponent<Text>().text = resourceName;
            row.transform.Find("Price").GetComponent<Text>().text = market.Value.MarketPrice.ToString();
            row.transform.Find("BuyButton").GetComponent<Button>().onClick.AddListener(PlayerBuyGood);
            row.transform.Find("SellButton").GetComponent<Button>().onClick.AddListener(PlayerSellGood);

            rows.Add(row, market.Value.Resource.Type);

        }

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {            
            townMenu.enabled = true;
        }
        
    }

    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            townMenu.enabled = false;
        }
    }

    public void UpdatePrices()
    {
        foreach (KeyValuePair<ResourceUtil.ResourceType, Market> market in townManager.town.Markets)
        {
            string resourceName = market.Value.Resource.DisplayName;
            float marketPrice = market.Value.MarketPrice;

            Transform row = priceMenuGrid.transform.Find(resourceName + "Row");
            row.transform.Find("Price").GetComponent<Text>().text = marketPrice.ToString();

            

        }

    }

    public void PlayerBuyGood()
    {      

        Transform row = EventSystem.current.currentSelectedGameObject.transform.parent;
        ResourceUtil.ResourceType type = rows[row];

        float price = townManager.town.Markets[type].MarketPrice;        
        List<Entity> availableAsks = townManager.town.Markets[type].AskLedger;

        if (PlayerManager.player.HasResource(type) == false)
        {
            PlayerManager.player.AddResource(type);
        }

        if (PlayerManager.player.ResourceInventory[type].Amount < PlayerManager.player.ResourceInventory[type].Max)
        {
            PlayerManager.player.IncrementResource(type);
            PlayerManager.player.Currency -= price;
            EventManager.TriggerEvent("UpdateCurrency");

            if (availableAsks.Count > 0)
            {
                availableAsks[0].Inventory[type].Offer.Amount -= 1;
                availableAsks[0].Inventory[type].Amount -= 1;
                availableAsks[0].Currency += price;

                if (availableAsks[0].Inventory[type].Offer.Amount < 1)
                {
                    availableAsks.RemoveAt(0);
                }
            }

        }


        Debug.Log(EventSystem.current.currentSelectedGameObject.name);

    }

    public void PlayerSellGood()
    {
        Transform row = EventSystem.current.currentSelectedGameObject.transform.parent;
        ResourceUtil.ResourceType type = rows[row];

        float price = townManager.town.Markets[type].MarketPrice;
        List<Entity> availableBids = townManager.town.Markets[type].BidLedger;

        if (PlayerManager.player.HasResource(type))
        {
            if (PlayerManager.player.ResourceInventory[type].Amount > 0)
            {
                PlayerManager.player.DecrementResource(type);
                PlayerManager.player.Currency += price;
                EventManager.TriggerEvent("UpdateCurrency");

                if (availableBids.Count > 0)
                {
                    availableBids[0].Inventory[type].Offer.Amount -= 1;
                    availableBids[0].Inventory[type].Amount += 1;
                    availableBids[0].Currency += price;

                    if (availableBids[0].Inventory[type].Offer.Amount < 1)
                    {
                        availableBids.RemoveAt(0);
                    }
                }


            }
        }

    }
}
