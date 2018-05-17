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

    private const float _productionPenaltyAmount = 5;

    private int _profitLedgerLimit = 1000;

    public float CurrencyLastRound { get; private set; }
    public List<float> ProfitLedger { get; set; }
    public Town Town;
    public Guild Guild { get; set; }
    public Resource CommodityProduced { get; set; }
    private int RoundsWithoutProduction { get; set; }
    public int TotalRoundsWithoutProduction { get; set; }
    public int RoundsWithoutTrading { get; set; }
    public int NumberOfAuctions { get; set; }

    public AgentEntity(string name, EntityType type) : base(name)
    {
        Random r = new Random(DateTime.Now.Millisecond);

        Name = name;
        Type = type;
        Currency = 500;
        CurrencyLastRound = Currency;
        Inventory = new Dictionary<ResourceUtil.ResourceType, InventoryItem>();
        Markets = new Dictionary<ResourceUtil.ResourceType, Market>();
        ProfitLedger = new List<float>();

    }

    public new void AddResource(Resource resource, InventoryItem.ActionType action, float max, float ideal, float amount = 0)
    {
        AddSelfToMarket(resource);

        float priceMin = (float)Math.Round(resource.BasePrice - resource.BasePrice * 0.5);
        float priceMax = (float)Math.Round(resource.BasePrice + resource.BasePrice * 0.5);
        PriceRange priceRange = new PriceRange(priceMin, priceMax);

        InventoryItem row = new InventoryItem(resource, action, priceRange, max, ideal, amount);

        if (action == InventoryItem.ActionType.sell)
        {
            CommodityProduced = resource;
            AddSelfToGuild(resource);
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

    public void AddSelfToGuild(Resource resource)
    {
        Town.AddAgentToGuild(this);
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
        DonateProfitsToGuild();
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

    public void DonateResourceToGuild(float amount = 1)
    {
        if (Inventory[CommodityProduced.Type].Amount >= amount)
        {
            Inventory[CommodityProduced.Type].Amount -= amount;
            Guild.AddToPool(amount);
        }
    }

    public void DonateProfitsToGuild()
    {
        float profits = ProfitLedger[ProfitLedger.Count - 1];
        float donation;

        if (profits > 0)
        {
            donation = (float)Math.Floor(profits * 0.30f);
            Guild.AddToProfits(donation);
        }
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
            RoundsWithoutProduction = 0;
        }
        else
        {
            ProductionPenalty();
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
        if (Inventory[ResourceUtil.ResourceType.wheat].Amount > 0 && Inventory[ResourceUtil.ResourceType.wood].Amount > 0)
        {
            float output = 2;
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

            Inventory[ResourceUtil.ResourceType.ore].Amount += amountProduced;
            Inventory[ResourceUtil.ResourceType.wheat].Amount -= 1;
            Inventory[ResourceUtil.ResourceType.wood].Amount -= 1;

            float wheatCost = Inventory[ResourceUtil.ResourceType.wheat].PriceRange.Mean;
            float woodCost = Inventory[ResourceUtil.ResourceType.wood].PriceRange.Mean;
            float toolsCost = Inventory[ResourceUtil.ResourceType.tools].PriceRange.Mean;

            Inventory[ResourceUtil.ResourceType.ore].CostToProduce = (float)Math.Round(wheatCost / amountProduced + woodCost / amountProduced + toolsCost * brokenTools);
            RoundsWithoutProduction = 0;
        }
        else
        {
            ProductionPenalty();
        }
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
            
            Inventory[ResourceUtil.ResourceType.metal].CostToProduce = (float)Math.Round(wheatCost / amountProduced + oreCost + toolsCost * brokenTools);
            RoundsWithoutProduction = 0;
        }
        else
        {
            ProductionPenalty();
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

            Inventory[ResourceUtil.ResourceType.tools].CostToProduce = (float)Math.Round(Inventory[ResourceUtil.ResourceType.wheat].PriceRange.Mean / amountProduced + Inventory[ResourceUtil.ResourceType.metal].PriceRange.Mean);
            RoundsWithoutProduction = 0;
        }
        else
        {
            ProductionPenalty();
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

    private void ProductionPenalty()
    {
        RoundsWithoutProduction++;

        int penaltyModifier = RoundsWithoutProduction / 5;

        float penaltyAmount = _productionPenaltyAmount;
        
        penaltyAmount += penaltyAmount * penaltyModifier;

        Currency -= penaltyAmount;
        Town.Currency += penaltyAmount;

        TotalRoundsWithoutProduction++;

    }
}
