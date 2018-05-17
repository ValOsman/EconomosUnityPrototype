using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Random = System.Random;

public class Village : Entity
{

    private const float AMOUNT_PRODUCED = 5;

    public float Population { get; set; }
    public Town Town { get; set; }
    public Resource Resource { get; set; }
    public String VillageType { get; set; }
    public float AmountSoldToPlayer { get; set; } = 0;
    public float MonthlyPlayerSaleLimit { get; set; } = 5;
    public InventoryItem ResourceProduced
    {
        get { return Inventory[Resource.Type]; }
    }

    public Village(String name, float population, Resource resource)
    {
        Name = name;
        Population = population;
        Resource = resource;
        VillageType = GetVillageType(Resource.Type);

        float price = (float)Math.Round(Resource.BasePrice / 2);

        PriceRange priceRange = new PriceRange(price, price);

        InventoryItem inventory = new InventoryItem(Resource, InventoryItem.ActionType.sell, priceRange, 20, 5, 5);

        Inventory.Add(Resource.Type, inventory);
    }


    private String GetVillageType(ResourceUtil.ResourceType resourceType)
    {
        switch(resourceType)
        {
            case ResourceUtil.ResourceType.wheat:
                return "Farmlands";
            case ResourceUtil.ResourceType.wood:
                return "Logging Camp";
            case ResourceUtil.ResourceType.ore:
                return "Mining Camp";
            default:
                return "Village";
        }
    }

    public void ProduceResource()
    {
        ResourceProduced.Amount += AMOUNT_PRODUCED;
    }
}
