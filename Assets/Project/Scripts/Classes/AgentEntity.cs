using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Random = System.Random;
using UnityEngine;

public class AgentEntity : Entity
{

    private const float _lowCommodityAmount = 0.10f;
    private const float _excessCommodityAmount = 0.50f;
    private const float _highCommodityAmount = 1.0f;

    private const float _overpayFactor = 0.5f;
    private const float _underchargingFactor = 0.5f;

    private int _profitLedgerLimit = 1000;

    public float CurrencyLastRound { get; private set; }
    public List<float> ProfitLedger { get; set; }
    public Town Town;
    public Resource CommodityProduced { get; set; }
    public int RoundsWithoutProduction { get; set; }
    public int RoundsWithoutTrading { get; set; }
    public int NumberOfAuctions { get; set; }

    public AgentEntity(string name, EntityType type) : base(name)
    {
        Random r = new Random(DateTime.Now.Millisecond);

        Name = name;
        Type = type;
        Currency = 1000;
        CurrencyLastRound = Currency;
        Inventory = new Dictionary<ResourceUtil.ResourceType, InventoryItem>();
        Markets = new Dictionary<ResourceUtil.ResourceType, Market>();
        ProfitLedger = new List<float>();

    }

    public new void AddResource(Resource resource, InventoryItem.ActionType action, float max, float ideal, float amount = 0)
    {
        AddSelfToMarket(resource);

        float priceMin = (float)Math.Round(resource.BasePrice - resource.BasePrice * 0.2);
        float priceMax = (float)Math.Round(resource.BasePrice + resource.BasePrice * 0.2);
        PriceRange priceRange = new PriceRange(priceMin, priceMax);

        InventoryItem row = new InventoryItem(resource, action, priceRange, max, ideal, amount);

        if (action == InventoryItem.ActionType.sell)
        {
            CommodityProduced = resource;
        }

        Inventory.Add(resource.Type, row);

    }

    public new void AddMarket(ResourceUtil.ResourceType type, Market market)
    {
        Markets.Add(type, market);
    }

    private void AddSelfToMarket(Resource resource)
    {
        Town.AddAgentToMarket(resource, this);
    }

    public new void RemoveMarket(ResourceUtil.ResourceType type)
    {
        Markets.Remove(type);
    }

    public void UpdateProfit()
    {
        float profit = Currency - CurrencyLastRound;
        CurrencyLastRound = Currency;
        AddProfit(profit);
    }

    private void AddProfit(float profit)
    {
        ProfitLedger.Add(profit);

        if (ProfitLedger.Count() > _profitLedgerLimit)
        {
            ProfitLedger.RemoveAt(0);
        }
    }

    public float ProfitToDate()
    {
        return ProfitLedger.Sum();
    }

    public void PerformProduction()
    {

        switch (Type)
        {
            case EntityType.farmer:
                ProduceFood();
                break;
            case EntityType.woodcutter:
                ProduceWood();
                break;
            case EntityType.miner:
                ProduceOre();
                break;
            case EntityType.smelter:
                ProduceMetal();
                break;
            case EntityType.blacksmith:
                ProduceTools();
                break;
        }
    }

    private void ProduceCommodity(ResourceUtil.ResourceType produce, ResourceUtil.ResourceType consume, float output)
    {
        if (Inventory[consume].Amount > 0)
        {
            float amountProduced;
            float brokenTools = 0;
            if (Inventory[ResourceUtil.ResourceType.tools].Amount > 0)
            {
                amountProduced = output * 2;
                if (BreakTools() == true)
                {
                    Inventory[ResourceUtil.ResourceType.tools].Amount -= 1;
                    brokenTools = 1;
                }

            }
            else
            {
                amountProduced = output;
            }

            Inventory[produce].Amount += amountProduced;
            Inventory[consume].Amount -= 1;

            float consumedResourceCost = Inventory[consume].PriceRange.Mean;
            float toolsCost = Inventory[ResourceUtil.ResourceType.tools].PriceRange.Mean;

            Inventory[produce].CostToProduce = (float)Math.Round(consumedResourceCost / amountProduced + toolsCost * brokenTools);

        }
        else
        {
            Currency -= 2;
            RoundsWithoutProduction++;
        }
    }

    private void ProduceFood()
    {

        ProduceCommodity(ResourceUtil.ResourceType.wheat, ResourceUtil.ResourceType.wood, 2);

    }

    private void ProduceWood()
    {
        ProduceCommodity(ResourceUtil.ResourceType.wood, ResourceUtil.ResourceType.wheat, 1);
    }

    private void ProduceOre()
    {
        ProduceCommodity(ResourceUtil.ResourceType.ore, ResourceUtil.ResourceType.wheat, 2);
    }

    private void ProduceMetal()
    {
        if (Inventory[ResourceUtil.ResourceType.wheat].Amount > 0
           && Inventory[ResourceUtil.ResourceType.ore].Amount > 0)
        {
            float amountProduced;
            float brokenTools = 0;
            if (Inventory[ResourceUtil.ResourceType.tools].Amount > 0)
            {
                amountProduced = Inventory[ResourceUtil.ResourceType.ore].Amount;
                if (BreakTools() == true)
                {
                    Inventory[ResourceUtil.ResourceType.tools].Amount -= 1;
                    brokenTools = 1;
                }
            }
            else
            {
                amountProduced = Math.Min(Inventory[ResourceUtil.ResourceType.ore].Amount, 2);
            }

            Inventory[ResourceUtil.ResourceType.metal].Amount += amountProduced;
            Inventory[ResourceUtil.ResourceType.ore].Amount -= amountProduced;
            Inventory[ResourceUtil.ResourceType.wheat].Amount -= 1;

            float wheatCost = Inventory[ResourceUtil.ResourceType.wheat].PriceRange.Mean;
            float oreCost = Inventory[ResourceUtil.ResourceType.ore].PriceRange.Mean;
            float toolsCost = Inventory[ResourceUtil.ResourceType.tools].PriceRange.Mean;



            Inventory[ResourceUtil.ResourceType.metal].CostToProduce = (float)Math.Round(wheatCost / amountProduced + oreCost * amountProduced + toolsCost * brokenTools);
        }
        else
        {
            Currency -= 2;
            RoundsWithoutProduction++;
        }

    }

    private void ProduceTools()
    {
        if (Inventory[ResourceUtil.ResourceType.wheat].Amount > 0 && Inventory[ResourceUtil.ResourceType.metal].Amount > 0)
        {
            float amountProduced;
            amountProduced = Inventory[ResourceUtil.ResourceType.metal].Amount;

            Inventory[ResourceUtil.ResourceType.tools].Amount += amountProduced;
            Inventory[ResourceUtil.ResourceType.metal].Amount = 0;
            Inventory[ResourceUtil.ResourceType.wheat].Amount -= 1;

            Inventory[ResourceUtil.ResourceType.tools].CostToProduce = (float)Math.Round(Inventory[ResourceUtil.ResourceType.wheat].PriceRange.Mean / amountProduced + Inventory[ResourceUtil.ResourceType.metal].PriceRange.Mean * amountProduced);
        }
        else
        {
            Currency = Currency - 2;
            RoundsWithoutProduction++;
        }
    }

    private bool BreakTools()
    {
        Random r = new Random(DateTime.Now.Millisecond);
        int result = r.Next(1, 11);
        if (result <= 1)
        {
            return true;
        }
        else
        {
            return false;
        }

    }
}
