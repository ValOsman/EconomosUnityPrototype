using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Market
{

    private int _priceHistoryLimit = 30;
    private int _marketHistoryLimit = 30;

    //private Random _random = new Random(DateTime.Now.Millisecond);

    public List<PriceAmountPair> BidBook { get; set; }
    public List<PriceAmountPair> AskBook { get; set; }
    public Resource Resource { get; private set; }
    public List<Entity> BidLedger { get; set; } = new List<Entity>();
    public List<Entity> AskLedger { get; set; } = new List<Entity>();
    public float MarketPrice { get; set; }
    public List<float> PriceHistory { get; set; } = new List<float>();
    public List<MarketHistoryRecord> MarketHistory { get; set; } = new List<MarketHistoryRecord>(); //round #, number of bids, number of asks, average clearing price
    public List<Entity> Agents { get; set; } = new List<Entity>();
    public List<Entity> Buyers { get; set; } = new List<Entity>();
    public List<Entity> Sellers { get; set; } = new List<Entity>();
    public float Demand { get; private set; } //average number of units demanded from acution each round
    public float Supply { get; private set; } //average number of units supplied for auction each round
    public Town Town { get; set; } // Town that the market belongs to
    public List<float> TransactionRecords { get; set; } //Make a TransactionRecord class with seller, buyer, amount, price, and round



    public Market(Resource resource)
    {
        Resource = resource;
        MarketPrice = resource.BasePrice;

        MarketHistory.Add(new MarketHistoryRecord(0, 1, 1, 1, 1));
        //populate market history with a single fake record to start
    }

    public void AssignTown(Town town)
    {
        Town = town;
    }

    public void AddAgent(AgentEntity agent)
    {
        Agents.Add(agent);
        agent.AddMarket(Resource.Type, this);
    }

    public void RemoveAgent(AgentEntity agent)
    {
        agent.RemoveMarket(Resource.Type);
        Agents.Remove(agent);
    }

    public void HoldAuctions()
    {
        MarketHistoryRecord record = new MarketHistoryRecord();
        List<float> clearingPrices = new List<float>(); //clearing prices in the current round

        BidLedger.Clear();
        AskLedger.Clear();

        foreach (AgentEntity agent in Agents)
        {
            agent.NumberOfAuctions++;
            if (agent.Inventory[Resource.Type].MakeOffer == true)
            {
                if (agent.Inventory[Resource.Type].Action == InventoryItem.ActionType.buy)
                {
                    if (agent.Inventory[Resource.Type].Offer.Amount > 0 && agent.Inventory[Resource.Type].Offer.TotalPrice < agent.Currency)
                    {
                        BidLedger.Add(agent);
                    }
                }
                else if (agent.Inventory[Resource.Type].Action == InventoryItem.ActionType.sell)
                {
                    if (agent.Inventory[Resource.Type].Offer.Amount > 0)
                    {
                        AskLedger.Add(agent);
                    }
                }
            }
            else
            {
                agent.RoundsWithoutTrading++;
            }

        }

        BidLedger.Shuffle();
        AskLedger.Shuffle();

        BidLedger = BidLedger.OrderByDescending(agent => agent.Inventory[Resource.Type].Offer.Price).ToList();
        AskLedger = AskLedger.OrderBy(agent => agent.Inventory[Resource.Type].Offer.Price).ToList();

        record.NumberOfAsks = AskLedger.Count();
        record.NumberOfBids = BidLedger.Count();


        while (BidLedger.Count > 0 && AskLedger.Count > 0)
        {
            float quantityTraded = Math.Min(BidLedger[0].Inventory[Resource.Type].Offer.Amount, AskLedger[0].Inventory[Resource.Type].Offer.Amount);
            float clearingPrice = (float)Math.Ceiling((BidLedger[0].Inventory[Resource.Type].Offer.Price + AskLedger[0].Inventory[Resource.Type].Offer.Price) / 2);

            record.Demand += (int)BidLedger[0].Inventory[Resource.Type].Offer.Amount;
            record.Supply += (int)AskLedger[0].Inventory[Resource.Type].Offer.Amount;

            if (quantityTraded > 0)
            {
                BidLedger[0].Inventory[Resource.Type].Offer.Amount -= quantityTraded;
                AskLedger[0].Inventory[Resource.Type].Offer.Amount -= quantityTraded;

                BidLedger[0].Inventory[Resource.Type].Amount += quantityTraded;
                AskLedger[0].Inventory[Resource.Type].Amount -= quantityTraded;

                BidLedger[0].Currency -= quantityTraded * clearingPrice;
                AskLedger[0].Currency += quantityTraded * clearingPrice;

                UpdatePriceHistory(clearingPrice);
                clearingPrices.Add(clearingPrice);

                BidLedger[0].UpdateBidPriceBelief(this, BidLedger[0].Inventory[Resource.Type], true);     //Buyer updates price belief
                AskLedger[0].UpdateAskPriceBelief(this, AskLedger[0].Inventory[Resource.Type], true);     //Seller updates price belief


                if (BidLedger[0].Inventory[Resource.Type].Offer.Amount == 0)
                {
                    BidLedger.RemoveAt(0);
                }

                if (AskLedger[0].Inventory[Resource.Type].Offer.Amount == 0)
                {
                    AskLedger.RemoveAt(0);
                }
            }
            else
            {
                Console.WriteLine("Sumtin' fucked up");
            }

        }

        record.MeanClearingPrice = (float)Math.Round(clearingPrices.Sum() / clearingPrices.Count());
        AddMarketHistoryRecord(record);


        // Go through offers left over and update their price beliefs, having been rejected
        foreach (AgentEntity agent in BidLedger)
        {
            agent.UpdateBidPriceBelief(this, agent.Inventory[Resource.Type], false);
        }

        foreach (AgentEntity agent in AskLedger)
        {
            agent.UpdateAskPriceBelief(this, agent.Inventory[Resource.Type], false);
        }
        

    }

    public void HoldTownAuctions()
    {

        MarketHistoryRecord record = new MarketHistoryRecord();
        List<float> clearingPrices = new List<float>(); //clearing prices in the current round

        if (Town.Inventory[Resource.Type].MakeOffer == true)
        {
            BidLedger.Add(Town);
        }

        foreach (AgentEntity agent in Agents.Where(x => x.Inventory[Resource.Type].Action == InventoryItem.ActionType.sell))
        {
            agent.NumberOfAuctions++;
            if (agent.Inventory[Resource.Type].MakeOffer == true && agent.Inventory[Resource.Type].Offer.Amount > 0)
            {
                AskLedger.Add(agent);
            }
            else
            {
                agent.RoundsWithoutTrading++;
            }

        }


        AskLedger.Shuffle();

        AskLedger = AskLedger.OrderBy(agent => agent.Inventory[Resource.Type].Offer.Price).ToList();

        while (BidLedger.Count > 0 && AskLedger.Count > 0)
        {
            float quantityTraded = Math.Min(BidLedger[0].Inventory[Resource.Type].Offer.Amount, AskLedger[0].Inventory[Resource.Type].Offer.Amount);
            float clearingPrice = (float)Math.Ceiling((BidLedger[0].Inventory[Resource.Type].Offer.Price + AskLedger[0].Inventory[Resource.Type].Offer.Price) / 2);

            if (quantityTraded > 0)
            {
                BidLedger[0].Inventory[Resource.Type].Offer.Amount -= quantityTraded;
                AskLedger[0].Inventory[Resource.Type].Offer.Amount -= quantityTraded;

                BidLedger[0].Inventory[Resource.Type].Amount += quantityTraded;
                AskLedger[0].Inventory[Resource.Type].Amount -= quantityTraded;

                BidLedger[0].Currency -= quantityTraded * clearingPrice;
                AskLedger[0].Currency += quantityTraded * clearingPrice;

                UpdatePriceHistory(clearingPrice);

                BidLedger[0].UpdateBidPriceBelief(this, BidLedger[0].Inventory[Resource.Type], true);     //Buyer updates price belief

                if (BidLedger[0].Inventory[Resource.Type].Offer.Amount == 0)
                {
                    BidLedger.RemoveAt(0);
                }

                if (AskLedger[0].Inventory[Resource.Type].Offer.Amount == 0)
                {
                    AskLedger.RemoveAt(0);
                }
            }
            else
            {
                Console.WriteLine("Sumtin' fucked up");
            }

        }

        // Go through offers left over and update their price beliefs, having been rejected

        BidLedger.Clear();
        AskLedger.Clear();

    }

    public void UpdatePriceHistory(float price)
    {
        PriceHistory.Add(price);
        if (PriceHistory.Count > _priceHistoryLimit)
        {
            PriceHistory.RemoveAt(0);
        }
        UpdatePriceMean();
    }

    public void UpdatePriceMean()
    {
        MarketPrice = (float)Math.Round(PriceHistory.Sum() / PriceHistory.Count);
    }

    public float AverageBids()
    {
        return (float)MarketHistory.Sum(round => round.NumberOfBids) / MarketHistory.Count();
    }

    public float AverageAsks()
    {
        return (float)MarketHistory.Sum(round => round.NumberOfAsks) / MarketHistory.Count();
    }

    public int BidsThisRound()
    {
        return MarketHistory[MarketHistory.Count() - 1].NumberOfBids;
    }

    public int AsksThisRound()
    {
        return MarketHistory[MarketHistory.Count() - 1].NumberOfAsks;
    }

    public int DemandThisRound()
    {
        return MarketHistory[MarketHistory.Count() - 1].Demand;
    }

    public int SupplyThisRound()
    {
        return MarketHistory[MarketHistory.Count() - 1].Supply;
    }


    private void AddMarketHistoryRecord(MarketHistoryRecord record)
    {
        MarketHistory.Add(record);
        if (MarketHistory.Count() > _marketHistoryLimit)
        {
            MarketHistory.RemoveAt(0);
        }

        Demand = (float)Math.Round((MarketHistory.Sum(x => x.Demand) / (float)MarketHistory.Count()), 2);
        Supply = (float)Math.Round((MarketHistory.Sum(x => x.Supply) / (float)MarketHistory.Count()), 2);

    }

    public class MarketHistoryRecord
    {
        public int Round { get; set; }
        public int NumberOfBids { get; set; }
        public int NumberOfAsks { get; set; }
        public int Supply { get; set; }
        public int Demand { get; set; }
        public float MeanClearingPrice { get; set; }

        public MarketHistoryRecord()
        {

        }

        public MarketHistoryRecord(int round, int numBids, int numAsks, int supply, int demand)
        {
            Round = round;
            NumberOfBids = numBids;
            NumberOfAsks = numAsks;
            Supply = supply;
            Demand = demand;
        }
    }

}