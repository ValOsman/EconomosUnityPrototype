using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ResourceUtil
{

    public enum ResourceType { wheat, wood, ore, metal, tools }

    public static Resource Wheat = new Resource(0, "Wheat", ResourceType.wheat, 2, AgentEntity.EntityType.farmer, Resource.ProductionDifficulty.trivial);
    public static Resource Wood = new Resource(1, "Wood", ResourceType.wood, 4, AgentEntity.EntityType.woodcutter, Resource.ProductionDifficulty.easy);
    public static Resource Ore = new Resource(2, "Ore", ResourceType.ore, 6, AgentEntity.EntityType.miner, Resource.ProductionDifficulty.easy);
    public static Resource Metal = new Resource(3, "Metal", ResourceType.metal, 8, AgentEntity.EntityType.smelter, Resource.ProductionDifficulty.medium);
    public static Resource Tools = new Resource(4, "Tools", ResourceType.tools, 10, AgentEntity.EntityType.blacksmith, Resource.ProductionDifficulty.medium);
    public static List<Resource> AllResources = new List<Resource>() { Wheat, Wood, Ore, Metal, Tools };


    public static Resource GetResourceByType(ResourceUtil.ResourceType type)
    {
        switch (type)
        {
            case ResourceType.wheat:
                return Wheat;
            case ResourceType.wood:
                return Wood;
            case ResourceType.ore:
                return Ore;
            case ResourceType.metal:
                return Metal;
            case ResourceType.tools:
                return Tools;
            default:
                throw new Exception();

        }
    }
}
