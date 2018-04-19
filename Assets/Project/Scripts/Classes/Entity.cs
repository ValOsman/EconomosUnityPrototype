using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public abstract class Entity
{
    private const float _overpayFactor = 0.5f;
    private const float _underchargingFactor = 0.5f;

    private const float _priceAdjustmentFactor = 0.10f;

    public enum EntityType { farmer, woodcutter, miner, smelter, blacksmith, town }

    public string Name { get; set; }
    public float Currency { get; set; }
    public int RoundCreated { get; private set; } = TimeUtil.Rounds;
    public AgentEntity.EntityType Type { get; set; }
    public Dictionary<ResourceUtil.ResourceType, InventoryItem> Inventory { get; set; }
    public Dictionary<ResourceUtil.ResourceType, Market> Markets;

    public Entity(string name)
    {
        Random r = new Random(DateTime.Now.Millisecond);

        Name = name;
        Currency = 200;
        Inventory = new Dictionary<ResourceUtil.ResourceType, InventoryItem>();
        Markets = new Dictionary<ResourceUtil.ResourceType, Market>();

    }

    public void AddResource(Resource resource, InventoryItem.ActionType action, float max, float ideal, float amount = 0)
    {
        float priceMin = (float)Math.Round(resource.BasePrice - resource.BasePrice * 0.2);
        float priceMax = (float)Math.Round(resource.BasePrice + resource.BasePrice * 0.2);
        PriceRange priceRange = new PriceRange(priceMin, priceMax);

        InventoryItem row = new InventoryItem(resource, action, priceRange, max, ideal, amount);

        Inventory.Add(resource.Type, row);
    }

    public void AddMarket(ResourceUtil.ResourceType type, Market market)
    {
        Markets.Add(type, market);
    }

    public void RemoveMarket(ResourceUtil.ResourceType type)
    {
        Markets.Remove(type);
    }

    public void GenerateOffers()
    {
        foreach (KeyValuePair<ResourceUtil.ResourceType, InventoryItem> item in Inventory)
        {
            float limit;
            float freeSpace = item.Value.FreeSpace();
            switch (item.Value.Action)
            {
                case InventoryItem.ActionType.buy:
                    float shortage = item.Value.Shortage();
                    if (item.Value.Shortage() > 0)
                    {
                        item.Value.MakeOffer = true;

                        if (freeSpace >= shortage)
                        {
                            limit = item.Value.Shortage();
                        }
                        else
                        {
                            limit = freeSpace;
                        }
                        item.Value.Offer = CreateBid(item.Value.Resource.Type, Markets[item.Value.Resource.Type].MarketPrice, limit);
                    }
                    else
                    {
                        item.Value.MakeOffer = false;
                    }
                    break;
                case InventoryItem.ActionType.sell:
                    float surplus = item.Value.Surplus();
                    if (surplus > 0)
                    {
                        item.Value.MakeOffer = true;
                        limit = surplus;
                        item.Value.Offer = CreateAsk(item.Value.Resource.Type, Markets[item.Value.Resource.Type].MarketPrice, limit);
                    }
                    else
                    {
                        item.Value.MakeOffer = false;
                    }
                    break;
            }
        }
    }

    //Buy at most limit number of items
    public PriceAmountPair CreateBid(ResourceUtil.ResourceType resourceType, float marketMean, float limit)
    {
        Random r = new Random(DateTime.Now.Millisecond);
        InventoryItem resource = Inventory[resourceType];

        float bidPrice = r.Next((int)resource.PriceRange.Min, (int)resource.PriceRange.Max);

        float idealQuantity = DeterminePurchaseQuantity(resource, marketMean);

        float amountToBuy = Math.Min(idealQuantity, limit);

        return new PriceAmountPair(bidPrice, amountToBuy);
    }

    //Sell at least limit number of items
    public PriceAmountPair CreateAsk(ResourceUtil.ResourceType resourceType, float marketMean, float limit)
    {
        Random r = new Random(DateTime.Now.Millisecond);
        InventoryItem resource = Inventory[resourceType];

        float priceFromRange = r.Next((int)resource.PriceRange.Min, (int)resource.PriceRange.Max);

        float askPrice = Math.Max(priceFromRange, Inventory[resourceType].CostToProduce);

        float idealQuantity = DetermineSaleQuantity(resource, marketMean);

        float amountToSell = Math.Max(idealQuantity, limit);

        return new PriceAmountPair(askPrice, amountToSell);
    }

    private float DeterminePurchaseQuantity(InventoryItem resource, float marketMean)
    {
        float mean = marketMean;

        float favorabilty = 1 - FindFavorability(resource.PriceRange, mean);

        float amountToBuy = (float)Math.Round(favorabilty * resource.Shortage());

        if (amountToBuy > resource.Max)
        {
            amountToBuy = resource.Max - resource.Amount;
        }

        if (amountToBuy < 1)
        {
            amountToBuy = 1;
        }

        return amountToBuy;
    }

    private float DetermineSaleQuantity(InventoryItem resource, float marketMean)
    {
        float mean = marketMean;

        float favorabilty = FindFavorability(resource.PriceRange, mean);

        float amountToSell = (float)Math.Round(favorabilty * resource.Surplus());

        if (amountToSell > resource.Amount)
        {
            amountToSell = resource.Amount;
        }

        if (amountToSell < 1)
        {
            amountToSell = 1;
        }

        return amountToSell;
    }

    public void UpdateBidPriceBelief(Market market, InventoryItem resource, bool bidAccepted)
    {
        resource.RecordCurrentOffer(bidAccepted);
        float meanDifference = resource.PriceRange.Mean - market.MarketPrice;
        float translationInterval = meanDifference * -1;
        float magnitude = _priceAdjustmentFactor;

        if (bidAccepted == true)
        {
            if (meanDifference >= resource.PriceRange.Mean * _overpayFactor) //overpaying
            {
                resource.PriceRange.Translate(translationInterval);
            }
            else
            {
                resource.PriceRange.ShrinkMax(magnitude);
            }
        }
        else if (bidAccepted == false)
        {
            if (resource.Amount < resource.Shortage()) //low on resource, so increase PriceRange
            {
                magnitude = magnitude * 2;
            }
            else //check supply vs demand
            {
                float demand = market.DemandThisRound();
                float supply = market.SupplyThisRound();

                float supplyDemandRatio = supply > 0 ? demand / supply : 0;

                if (supplyDemandRatio > 1) //More demand than supply
                {
                    //Try to anticipate new mean price and move towards it ahead of time
                    float newMean = market.MarketPrice + market.MarketPrice * (1 / supplyDemandRatio);
                    meanDifference = resource.PriceRange.Mean - newMean;
                    translationInterval = meanDifference * -1;
                }
            }

            if (translationInterval > 0)
            {
                float translationMagnitude = 0.5f;
                if (resource.OfferHistory.Count >= 6)
                {
                    translationMagnitude = 1 - resource.GetTradeSuccessRatio();
                }

                resource.PriceRange.Translate(translationInterval, translationMagnitude);
            }

            resource.PriceRange.GrowMax(magnitude);
        }
    }

    public void UpdateAskPriceBelief(Market market, InventoryItem resource, bool askAccepted)
    {
        resource.RecordCurrentOffer(askAccepted);
        float meanDifference = resource.PriceRange.Mean - market.MarketPrice;
        float translationInterval = meanDifference * -1;
        float magnitude = _priceAdjustmentFactor;

        if (askAccepted == true)
        {
            if (meanDifference <= resource.PriceRange.Mean * _underchargingFactor) //undercharging
            {
                resource.PriceRange.Translate(translationInterval);
            }
            else
            {
                resource.PriceRange.ShrinkMin(magnitude);
            }
        }
        else if (askAccepted == false)
        {
            if (resource.FreeSpace() <= resource.Max * 0.20f) //inventory is nearing capacity, so increase PriceRange
            {
                magnitude = magnitude * 2;
            }
            else //check supply vs demand
            {
                float demand = market.DemandThisRound();
                float supply = market.SupplyThisRound();

                float supplyDemandRatio = demand > 0 ? supply / demand : 0;

                if (supplyDemandRatio >= 1) //More supply than demand
                {
                    //Try to anticipate new mean price and move towards it ahead of time
                    float newMean = market.MarketPrice - market.MarketPrice * (1 / supplyDemandRatio);
                    meanDifference = resource.PriceRange.Mean - newMean;
                    translationInterval = meanDifference * -1;
                }
            }

            if (translationInterval < 0)
            {
                float translationMagnitude = 0.5f;
                if (resource.OfferHistory.Count >= 6)
                {
                    translationMagnitude = 1 - resource.GetTradeSuccessRatio();
                }

                resource.PriceRange.Translate(translationInterval, translationMagnitude);
            }

            resource.PriceRange.GrowMin(magnitude);
        }
    }

    public float FindFavorability(PriceRange range, float mean)
    {
        if (mean > range.Max)
        {
            return 1;
        }
        else if (mean < range.Min)
        {
            return 0;
        }

        float position = mean - range.Min + 1;
        float max = range.Max - range.Min + 1;

        float percentage = position / max;

        return (float)Math.Round(percentage, 2);

    }

}
