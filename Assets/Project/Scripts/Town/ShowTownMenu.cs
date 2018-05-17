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

            string buyPrice = townManager.town.Guilds[market.Value.Resource.ProducedBy].Price.ToString();
            string sellPrice = market.Value.MarketPrice.ToString();

            Transform buyPanel = row.transform.Find("BuyPanel");
            Transform sellPanel = row.transform.Find("SellPanel");

            row.transform.Find("ResourceLabel").GetComponent<Text>().text = resourceName;
            buyPanel.transform.Find("BuyPrice").GetComponent<Text>().text = buyPrice;
            sellPanel.transform.Find("SellPrice").GetComponent<Text>().text = sellPrice;

            //row.transform.Find("BuyPanel").transform.Find("BuyButton").GetComponent<Text>().text = buyPrice;
            //row.transform.Find("SellPanel").transform.Find("SellButton").GetComponent<Text>().text = sellPrice;
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
        foreach (KeyValuePair<ResourceUtil.ResourceType, Market> market in townManager.town.Markets)
        {
            Guild guild = townManager.town.Guilds[market.Value.Resource.ProducedBy];
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
        Guild guild = townManager.town.Guilds[agentType];

        float price = guild.Price;
    
        
        if (PlayerManager.player.HasResource(type) == false)
        {
            PlayerManager.player.AddResource(type);
        }

        if (PlayerManager.player.ResourceInventory[type].Amount < PlayerManager.player.ResourceInventory[type].Max)
        {
            guild.TransferResource(PlayerManager.player, type, 1);
            PlayerManager.player.TransferCurrency(guild, price);
            EventManager.TriggerEvent("UpdateCurrency");
        }


        Debug.Log(EventSystem.current.currentSelectedGameObject.name);

    }

    public void PlayerSellGood()
    {
        Transform row = EventSystem.current.currentSelectedGameObject.transform.parent.transform.parent;
        ResourceUtil.ResourceType type = rows[row];

        float price = townManager.town.Markets[type].MarketPrice;
        
        if (PlayerManager.player.HasResource(type))
        {
            if (PlayerManager.player.ResourceInventory[type].Amount > 0)
            {
                PlayerManager.player.TransferResource(townManager.town, type, 1);
                townManager.town.TransferCurrency(PlayerManager.player, price);
                EventManager.TriggerEvent("UpdateCurrency");                
            }
        }

    }
}
