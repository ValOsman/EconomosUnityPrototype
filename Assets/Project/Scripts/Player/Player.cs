using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player
{
    public string Name { get; set; }
    public float Currency { get; set; } = 500;
    public Dictionary<ResourceUtil.ResourceType, PlayerResourceItem> ResourceInventory { get; set; } = new Dictionary<ResourceUtil.ResourceType, PlayerResourceItem>();

    public Player()
    {

    }

    public class PlayerResourceItem
    {
        public Resource Resource;
        public float Amount { get; set; }
        public float Max { get; set; }

        public PlayerResourceItem(Resource resource, float amount = 0, float max = 0)
        {
            Resource = resource;
            Amount = amount;
            Max = max;
        }
    }

    public void IncrementResource(ResourceUtil.ResourceType type, float amount = 1)
    {
        if (HasResource(type))
        {
            ResourceInventory[type].Amount += amount;
        }
        else
        {
            AddResource(type, amount: amount);
        }

        EventManager.TriggerEvent("UpdateInventory");
    }

    public void DecrementResource(ResourceUtil.ResourceType type, float amount = 1)
    {
        ResourceInventory[type].Amount -= amount;

        EventManager.TriggerEvent("UpdateInventory");
    }

    public void AddResource(ResourceUtil.ResourceType type, float amount = 0, float max = 5)
    {
        ResourceInventory.Add(type, new PlayerResourceItem(ResourceUtil.GetResourceByType(type), amount, max));
    }

    public bool HasResource(ResourceUtil.ResourceType type)
    {
        if (ResourceInventory.ContainsKey(type))
        {
            return true;
        }
        else
        {
            return false;
        }
    }


}
