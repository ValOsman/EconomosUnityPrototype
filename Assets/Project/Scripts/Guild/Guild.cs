using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Random = System.Random;
using UnityEngine;

public class Guild : Entity
{
    public int Id { get; private set; }
    public List<AgentEntity> Members { get; set; } = new List<AgentEntity>();
    public Town Town { get; set; }
    public AgentEntity Guildmaster { get; set; }
    public Resource Resource { get; private set; }
    public InventoryItem ResourcePool
    {
        get { return Inventory[Resource.Type]; }
    }
    public float Price { get; set; }
    public float Profits { get; set; }

    public Guild(AgentEntity guildmaster, Town town, string name = "")
    {
        Town = town;
        Currency = 100;
        Resource = guildmaster.CommodityProduced;
        Guildmaster = guildmaster;
        Members.Add(guildmaster);

        // Eventually make this autogenerate a name in the style of Dwarf Fortress, e.g. "The Venerable Order of Blacksmiths of Albrook"
        string agentType = StringUtil.FirstCharToUpper(guildmaster.Type.ToString());
        Name = String.Format("The Guild of {0}s of {1}", agentType, Town.Name);

        float price = Town.Markets[Resource.Type].MarketPrice;
        PriceRange priceRange = new PriceRange(price, price);
        InventoryItem inventoryItem = new InventoryItem(Resource, InventoryItem.ActionType.sell, priceRange, 30, 0, 0);
        Inventory = new Dictionary<ResourceUtil.ResourceType,InventoryItem>();
        Inventory.Add(Resource.Type, inventoryItem);        
        Price = price;

    }

    public void AddMember(AgentEntity agent)
    {
        Members.Add(agent);
    }

    public int MemberCount()
    {
        return Members.Count();
    }

    public void AddToPool(float amount = 1)
    {
        ResourcePool.Amount += amount;
    }

    public void AddToProfits(float amount)
    {
        Profits += amount;
    }

    public void SetPrice()
    {
        // generate price with some shit
        float priceSum = Members.Sum(agent => agent.Inventory[Resource.Type].PriceRange.Mean);

        float price = (float)Math.Ceiling(priceSum / Members.Count);

        price = Math.Max(price, Town.Markets[Resource.Type].MarketPrice);

        ResourcePool.PriceRange.Min = price;
        ResourcePool.PriceRange.Max = price;
        Price = price;

    }

    public void DisburseProfits()
    {
        if (Profits < Members.Count)
        {
            float difference = Members.Count - Profits;
            Currency -= difference;
            Profits += difference;
        }

        float remainder = Profits % Members.Count;
        float disbursement = (Profits - remainder) / Members.Count;
        Currency += remainder;

        foreach(Entity member in Members)
        {
            member.Currency += disbursement;
        }

        Profits = 0;
    }




}
