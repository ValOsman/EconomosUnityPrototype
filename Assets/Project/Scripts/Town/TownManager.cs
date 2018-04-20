using System;
using Random = System.Random;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class TownManager : MonoBehaviour {

    public Town town;

    private UnityAction roundUpdateListener;

    private ShowTownMenu menu;

	// Use this for initialization
	void Awake () {
        menu = gameObject.GetComponent<ShowTownMenu>();

        Random r = new Random(DateTime.Now.Millisecond);
        int townNumber = r.Next(1, 100);

        town = new Town("Town" + townNumber);

        town.AddMarket(ResourceUtil.Wheat);
        town.AddMarket(ResourceUtil.Wood);
        town.AddMarket(ResourceUtil.Ore);
        town.AddMarket(ResourceUtil.Metal);
        town.AddMarket(ResourceUtil.Tools);

        roundUpdateListener = new UnityAction(RoundActions);
        EventManager.StartListening("UpdateRound", roundUpdateListener);

        for (int i = 0; i < 5; i++)
        {
            town.SpawnAgent(Entity.EntityType.farmer);
            town.SpawnAgent(Entity.EntityType.woodcutter);
            town.SpawnAgent(Entity.EntityType.miner);
            town.SpawnAgent(Entity.EntityType.smelter);
            town.SpawnAgent(Entity.EntityType.blacksmith);
        }

    }

    private void Start()
    {
        for (int i = 0; i < 100; i++)
        {
            foreach (AgentEntity agent in town.Agents)
            {
                agent.PerformProduction();
            }

            town.RemoveBankruptAgents();
            town.ResolveAuctions();
            town.UpdateProfits();            
        }

        menu.UpdatePrices();

    }

    // Update is called once per frame
    void Update () {
		
	}

    //These actions and checks occur once every round
    private void RoundActions()
    {
        if (TimeUtil.Day % 10 == 0)
        {
            town.CollectTaxes(10);
        }

        if (TimeUtil.Day == 30)
        {
            town.CheckRequirements();
        }

        foreach (AgentEntity agent in town.Agents)
        {
            agent.PerformProduction();
        }

        town.RemoveBankruptAgents();
        town.ResolveAuctions();
        town.UpdateProfits();

        menu.UpdatePrices();
    }
}
