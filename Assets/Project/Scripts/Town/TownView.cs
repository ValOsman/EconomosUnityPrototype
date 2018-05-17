using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TownView : MonoBehaviour {

    [SerializeField]
    private Canvas townMenu;
    private Transform priceMenuGrid;
    private Dictionary<Transform, ResourceUtil.ResourceType> rows;
    public RectTransform priceMenuRow;

    private TownController townController;

    private void Start()
    {
        townMenu.enabled = false;
        townController = gameObject.GetComponent<TownController>();
        rows = new Dictionary<Transform, ResourceUtil.ResourceType>();
        priceMenuGrid = townMenu.transform.Find("PriceGrid");

        foreach (KeyValuePair<ResourceUtil.ResourceType, Market> market in townController.town.Markets)
        {
            string resourceName = market.Value.Resource.DisplayName;
            Transform row = Instantiate(priceMenuRow, priceMenuGrid.transform, false);
            row.name = resourceName + "Row";

            string buyPrice = townController.town.Guilds[market.Value.Resource.ProducedBy].Price.ToString();
            string sellPrice = market.Value.MarketPrice.ToString();

            Transform buyPanel = row.transform.Find("BuyPanel");
            Transform sellPanel = row.transform.Find("SellPanel");

            row.transform.Find("ResourceLabel").GetComponent<Text>().text = resourceName;
            buyPanel.transform.Find("BuyPrice").GetComponent<Text>().text = buyPrice;
            sellPanel.transform.Find("SellPrice").GetComponent<Text>().text = sellPrice;

            buyPanel.transform.Find("BuyButton").GetComponent<Button>().onClick.AddListener(PlayerBuyGood);
            sellPanel.transform.Find("SellButton").GetComponent<Button>().onClick.AddListener(PlayerSellGood);

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
        foreach (KeyValuePair<ResourceUtil.ResourceType, Market> market in townController.town.Markets)
        {
            Guild guild = townController.town.Guilds[market.Value.Resource.ProducedBy];
            string resourceName = market.Value.Resource.DisplayName;
            float marketPrice = market.Value.MarketPrice;
            float guildPrice = guild.Price;

            string buyPrice = guildPrice.ToString();
            string sellPrice = marketPrice.ToString();

            Transform row = priceMenuGrid.transform.Find(resourceName + "Row");
            row.transform.Find("BuyPanel").transform.Find("BuyPrice").GetComponent<Text>().text = buyPrice;
            row.transform.Find("SellPanel").transform.Find("SellPrice").GetComponent<Text>().text = sellPrice;
            
        }
                
    }

    public void PlayerBuyGood()
    {      

        Transform row = EventSystem.current.currentSelectedGameObject.transform.parent.transform.parent;
        ResourceUtil.ResourceType type = rows[row];
        Entity.EntityType agentType = ResourceUtil.GetResourceByType(type).ProducedBy;
        Guild guild = townController.town.Guilds[agentType];

        float price = guild.Price;
    
        
        if (PlayerController.player.HasResource(type) == false)
        {
            PlayerController.player.AddResource(type);
        }

        if (PlayerController.player.ResourceInventory[type].Amount < PlayerController.player.ResourceInventory[type].Max)
        {
            guild.TransferResource(PlayerController.player, type, 1);
            PlayerController.player.TransferCurrency(guild, price);
            EventManager.TriggerEvent("UpdateCurrency");
        }


        Debug.Log(EventSystem.current.currentSelectedGameObject.name);

    }

    public void PlayerSellGood()
    {
        Transform row = EventSystem.current.currentSelectedGameObject.transform.parent.transform.parent;
        ResourceUtil.ResourceType type = rows[row];

        float price = townController.town.Markets[type].MarketPrice;
        
        if (PlayerController.player.HasResource(type))
        {
            if (PlayerController.player.ResourceInventory[type].Amount > 0)
            {
                PlayerController.player.TransferResource(townController.town, type, 1);
                townController.town.TransferCurrency(PlayerController.player, price);
                EventManager.TriggerEvent("UpdateCurrency");                
            }
        }

    }
}
