using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Random = System.Random;

public class Town : Entity
{

    public string Id { get; private set; }
    public List<AgentEntity> Agents { get; set; }
    public List<Resource> Resources { get; set; }
    public Dictionary<Entity.EntityType, Guild> Guilds { get; set; }
    public float Population { get; set; }
    public Dictionary<AgentEntity.EntityType, int> AgentTypeCount { get; set; }
    public List<KeyValuePair<ResourceUtil.ResourceType, float>> PriceModifiers { get; set; }
    public List<KeyValuePair<ResourceUtil.ResourceType, float>> ProductionModifiers { get; set; }
    public float ResourceRequirementRatio { get; set; }
    public Tuple<float, float> IncomeRatio { get; set; } //earn X dollars for every Y population

    public Town(string name) : base(name)
    {
        Name = name;
        Type = EntityType.town;
        Markets = new Dictionary<ResourceUtil.ResourceType, Market>();
        Guilds = new Dictionary<Entity.EntityType, Guild>();
        Population = 100;
        Currency = Population * 100;
        Agents = new List<AgentEntity>();
        AgentTypeCount = new Dictionary<AgentEntity.EntityType, int>();
        Inventory = new Dictionary<ResourceUtil.ResourceType, InventoryItem>();
        PriceModifiers = new List<KeyValuePair<ResourceUtil.ResourceType, float>>();
        ProductionModifiers = new List<KeyValuePair<ResourceUtil.ResourceType, float>>();
        ResourceRequirementRatio = 0.10f;
        IncomeRatio = new Tuple<float, float>(5, 1);
    }

    public void AddMarket(Resource resource)
    {
        Market market = new Market(resource);
        Markets.Add(resource.Type, market);
        market.AssignTown(this);

        AddResource(resource);
    }

    private void AddResource(Resource resource)
    {
        float maxResource = Population; //maybe max amount of inventory is dictated by storehouses
        float idealResource = RequiredResourceAmount();
        float min = (float)Math.Round(ResourceUtil.GetResourceByType(resource.Type).BasePrice / 2);
        float max = (float)Math.Round(ResourceUtil.GetResourceByType(resource.Type).BasePrice * 2);
        PriceRange priceRange = new PriceRange(min, max);
        Inventory.Add(resource.Type, new InventoryItem(resource, InventoryItem.ActionType.buy, priceRange, maxResource, idealResource));
    }

    private float RequiredResourceAmount()
    {
        return Population * ResourceRequirementRatio;
    }

    public void CheckRequirements()
    {
        float pass = 0;
        foreach (KeyValuePair<ResourceUtil.ResourceType, InventoryItem> item in Inventory)
        {
            if (item.Value.Amount >= RequiredResourceAmount() * 0.9f)
            {
                pass++;
            }
            item.Value.Amount = 0;
        }

        Population += (float)Math.Round(Population * (pass / Inventory.Count() - 0.2f));
        if (pass / Inventory.Count() >= 0.75f)
        {
            SpawnRandomAgent();
        }

    }

    public void AddAgentToMarket(Resource resource, AgentEntity agent)
    {
        Markets[resource.Type].AddAgent(agent);
    }

    public void AddAgentToGuild(AgentEntity agent)
    {
        if (Guilds.ContainsKey(agent.Type) == true)
        {
            Guilds[agent.Type].AddMember(agent);
        }
        else
        {
            Guild guild = new Guild(agent, this);
            Guilds.Add(agent.Type, guild);
        }

        agent.Guild = Guilds[agent.Type];

    }

    public void RemoveBankruptAgents()
    {
        AgentEntity removedAgent;
        List<Market> removedAgentsMarkets = new List<Market>();

        for (int i = 0; i < Agents.Count; i++)
        {
            if (Agents[i].Currency <= 0)
            {
                removedAgent = Agents[i];
                removedAgentsMarkets = removedAgent.Markets.Values.ToList();
                RemoveAgent(removedAgent);
                if (Agents.Count(x => x.Type == removedAgent.Type) < 1)
                {
                    SpawnAgent(removedAgent.Type);
                }
                else
                {
                    SpawnMostProfitableAgent();
                }
                
            }
        }
    }

    public void SpawnMostProfitableAgent()
    {
        SpawnAgent(MostProfitableAgentType());
    }

    public void SpawnRandomAgent()
    {
        AgentEntity.EntityType[] agentTypes = (AgentEntity.EntityType[])Enum.GetValues(typeof(AgentEntity.EntityType));
        Random r = new Random(DateTime.Now.Millisecond);
        int result = r.Next(agentTypes.Count() - 1);

        SpawnAgent(agentTypes[result]);
    }

    public void SpawnMostDemandedAgent()
    {
        SpawnAgent(MostDemandedAgentType());
    }

    public void GenerateAgentOffers()
    {
        foreach (AgentEntity agent in Agents)
        {
            agent.GenerateOffers();
        }
    }

    public void UpdateProfits()
    {
        foreach (AgentEntity agent in Agents)
        {
            agent.UpdateProfit();
        }
    }

    // For every Item2 people in the town, the town collects Item1 units of currency, multiplied by the number of days that have passed since last collecting taxes.
    public void CollectTaxes(int days)
    {
        Currency += (float)Math.Round(IncomeRatio.Item1 * (Population / IncomeRatio.Item2) * days);
    }

    public void ResolveAuctions()
    {
        GenerateAgentOffers();
        GenerateOffers();
        foreach (KeyValuePair<ResourceUtil.ResourceType, Market> market in Markets)
        {
            market.Value.HoldAuctions();
        }
    }

    public void ResolveTownAuctions()
    {
        GenerateAgentOffers();
        GenerateOffers();
        foreach (KeyValuePair<ResourceUtil.ResourceType, Market> market in Markets)
        {
            market.Value.HoldTownAuctions();
        }

    }

    public void DisburseGuildProfits()
    {
        foreach(KeyValuePair<Entity.EntityType, Guild> guild in Guilds)
        {
            guild.Value.DisburseProfits();
        }
    }

    public void UpdateGuildPrices()
    {
        foreach (KeyValuePair<Entity.EntityType, Guild> guild in Guilds)
        {
            guild.Value.SetPrice();
        }
    }

    public void GeneratePlayerPricing()
    {
        //DO THIS
    }

    private void IncrementAgentType(AgentEntity.EntityType type)
    {
        if (AgentTypeCount.ContainsKey(type))
        {
            AgentTypeCount[type]++;
        }
        else
        {
            AgentTypeCount.Add(type, 1);
        }
    }

    private AgentEntity.EntityType MostProfitableAgentType()
    {
        float sumOfProfits = 0;
        float meanProfit = 0;
        float highestMeanProfit = 0;
        List<AgentEntity> listOfType = new List<AgentEntity>();
        AgentEntity.EntityType mostProfitable = new AgentEntity.EntityType();

        foreach (AgentEntity.EntityType type in Enum.GetValues(typeof(AgentEntity.EntityType)))
        {
            listOfType = Agents.Where(x => x.Type == type).ToList();
            foreach (AgentEntity agent in listOfType)
            {
                sumOfProfits += agent.ProfitLedger.Skip(Math.Max(0, agent.ProfitLedger.Count() - 15)).ToList().Sum();
            }

            int numberOfType = Agents.Count(x => x.Type == type);

            if (numberOfType > 0)
            {
                meanProfit = sumOfProfits / Agents.Count(x => x.Type == type);
                if (meanProfit > highestMeanProfit)
                {
                    highestMeanProfit = meanProfit;
                    mostProfitable = type;
                }
            }

        }

        return mostProfitable;
    }

    private AgentEntity.EntityType MostDemandedAgentType()
    {
        float difference = 0;
        float greatestDifference = 0;
        AgentEntity.EntityType mostDemandedAgentType = new AgentEntity.EntityType();

        foreach (KeyValuePair<ResourceUtil.ResourceType, Market> market in Markets)
        {
            difference = market.Value.RecentDemandAverage() - market.Value.RecentSupplyAverage();

            if (difference > 0 && difference > greatestDifference)
            {
                greatestDifference = difference;
                mostDemandedAgentType = market.Value.Resource.ProducedBy;
            }

        }

        return mostDemandedAgentType;

    }

    public Dictionary<AgentEntity.EntityType, int> GetAgentDistribution()
    {
        Dictionary<AgentEntity.EntityType, int> agentDistribution = new Dictionary<AgentEntity.EntityType, int>();
        foreach (AgentEntity.EntityType type in Enum.GetValues(typeof(AgentEntity.EntityType)))
        {
            agentDistribution.Add(type, Agents.Where(x => x.Type == type).Count());
        }

        return agentDistribution;

    }

    public void RemoveAgent(AgentEntity agent)
    {
        Console.WriteLine(String.Format("Town: {0}; Agent: {1}", Name, agent.Name));
        List<Market> agentMarkets = agent.Markets.Values.ToList();
        foreach (Market market in agentMarkets)
        {
            market.RemoveAgent(agent);
        }
        Guilds[agent.Type].RemoveMember(agent);
        Agents.Remove(agent);
    }

    public void SpawnAgent(AgentEntity.EntityType type)
    {
        IncrementAgentType(type);
        switch (type)
        {
            case AgentEntity.EntityType.farmer:
                SpawnFarmer();
                break;
            case AgentEntity.EntityType.woodcutter:
                SpawnWoodcutter();
                break;
            case AgentEntity.EntityType.miner:
                SpawnMiner();
                break;
            case AgentEntity.EntityType.smelter:
                SpawnSmelter();
                break;
            case AgentEntity.EntityType.blacksmith:
                SpawnBlacksmith();
                break;
        }
        AgentEntity newAgent = Agents[Agents.Count - 1];
        AddAgentToGuild(newAgent);
    }

    private void SpawnFarmer()
    {
        AgentEntity farmer = new AgentEntity("Farmer" + String.Format("{0:D3}", AgentTypeCount[AgentEntity.EntityType.farmer]), AgentEntity.EntityType.farmer);
        farmer.Town = this;
        farmer.AddResource(ResourceUtil.Wheat, InventoryItem.ActionType.sell, 10, 0);
        farmer.AddResource(ResourceUtil.Wood, InventoryItem.ActionType.buy, 10, 5, 2);
        farmer.AddResource(ResourceUtil.Tools, InventoryItem.ActionType.buy, 10, 2, 1);

        Agents.Add(farmer);

    }

    private void SpawnWoodcutter()
    {
        AgentEntity woodcutter = new AgentEntity("Woodcutter" + String.Format("{0:D3}", AgentTypeCount[AgentEntity.EntityType.woodcutter]), AgentEntity.EntityType.woodcutter);
        woodcutter.Town = this;
        woodcutter.AddResource(ResourceUtil.Wood, InventoryItem.ActionType.sell, 10, 0);
        woodcutter.AddResource(ResourceUtil.Wheat, InventoryItem.ActionType.buy, 10, 4, 1);
        woodcutter.AddResource(ResourceUtil.Tools, InventoryItem.ActionType.buy, 10, 2, 1);

        Agents.Add(woodcutter);
    }

    private void SpawnMiner()
    {
        AgentEntity miner = new AgentEntity("Miner" + String.Format("{0:D3}", AgentTypeCount[AgentEntity.EntityType.miner]), AgentEntity.EntityType.miner);
        miner.Town = this;
        miner.AddResource(ResourceUtil.Ore, InventoryItem.ActionType.sell, 10, 0);
        miner.AddResource(ResourceUtil.Wheat, InventoryItem.ActionType.buy, 10, 4, 1);
        miner.AddResource(ResourceUtil.Wood, InventoryItem.ActionType.buy, 10, 4, 1);
        miner.AddResource(ResourceUtil.Tools, InventoryItem.ActionType.buy, 10, 2, 1);

        Agents.Add(miner);
    }

    private void SpawnSmelter()
    {
        AgentEntity smelter = new AgentEntity("Smelter" + String.Format("{0:D3}", AgentTypeCount[AgentEntity.EntityType.smelter]), AgentEntity.EntityType.smelter);
        smelter.Town = this;
        smelter.AddResource(ResourceUtil.Metal, InventoryItem.ActionType.sell, 10, 0);
        smelter.AddResource(ResourceUtil.Ore, InventoryItem.ActionType.buy, 10, 4, 4);
        smelter.AddResource(ResourceUtil.Wheat, InventoryItem.ActionType.buy, 10, 4, 2);
        smelter.AddResource(ResourceUtil.Tools, InventoryItem.ActionType.buy, 10, 2, 1);

        Agents.Add(smelter);
    }

    private void SpawnBlacksmith()
    {
        AgentEntity blacksmith = new AgentEntity("Blacksmith" + String.Format("{0:D3}", AgentTypeCount[AgentEntity.EntityType.blacksmith]), AgentEntity.EntityType.blacksmith);
        blacksmith.Town = this;
        blacksmith.AddResource(ResourceUtil.Tools, InventoryItem.ActionType.sell, 10, 0);
        blacksmith.AddResource(ResourceUtil.Wheat, InventoryItem.ActionType.buy, 10, 4, 2);
        blacksmith.AddResource(ResourceUtil.Metal, InventoryItem.ActionType.buy, 10, 4, 4);

        Agents.Add(blacksmith);
    }

}
