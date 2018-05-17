using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class Market
{

    private int _priceHistoryLimit = 30;
    private int _marketHistoryLimit = 30;
    private int _transactionHistoryLimit = 10000;

    private Random _random = new Random(DateTime.Now.Millisecond);

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
    public float Demand { get; private set; } //average number of units demanded from auction each round
    public float Supply { get; private set; } //average number of units supplied for auction each round
    public Town Town { get; set; } // Town that the market belongs to
    public int LastRoundTraded { get; set; } = 1;
    public List<TransactionRecord> TransactionHistory { get; set; } = new List<TransactionRecord>(); //Make a TransactionRecord class with seller, buyer, amount, price, and round       

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
        BidLedger.Clear();
        AskLedger.Clear();

        MarketHistoryRecord record = new MarketHistoryRecord();
        List<float> clearingPrices = new List<float>(); //clearing prices in the current round

        foreach (AgentEntity agent in Agents)
        {
            agent.NumberOfAuctions++;
            if (agent.Inventory[Resource.Type].MakeOffer == true && agent.Inventory[Resource.Type].Offer != null)
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

        if (Town.Inventory[Resource.Type].MakeOffer == true)
        {
            BidLedger.Add(Town);
        }

        BidLedger.Shuffle();
        AskLedger.Shuffle();

        BidLedger = BidLedger.OrderByDescending(agent => agent.Inventory[Resource.Type].Offer.Price).ToList();
        AskLedger = AskLedger.OrderBy(agent => agent.Inventory[Resource.Type].Offer.Price).ToList();

        record.NumberOfAsks = AskLedger.Count();
        record.NumberOfBids = BidLedger.Count();

        record.Demand = (int)BidLedger.Sum(x => x.Inventory[Resource.Type].Offer.Amount);
        record.Supply = (int)AskLedger.Sum(x => x.Inventory[Resource.Type].Offer.Amount);

        if (BidLedger.Count > 0 && AskLedger.Count > 0)
        {
            LastRoundTraded = TimeUtil.Rounds;
        }
        

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
                clearingPrices.Add(clearingPrice);

                AddTransactionRecord(new TransactionRecord(BidLedger[0], AskLedger[0], clearingPrice, quantityTraded, Resource));

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

        record.MeanClearingPrice = clearingPrices.Count() > 0 ? (float)Math.Round(clearingPrices.Sum() / clearingPrices.Count()) : 0;
        AddMarketHistoryRecord(record);


        // Go through offers left over and update their price beliefs, having been rejected
        foreach (Entity agent in BidLedger)
        {
            agent.UpdateBidPriceBelief(this, agent.Inventory[Resource.Type], false);
        }

        foreach (Entity agent in AskLedger)
        {
            agent.UpdateAskPriceBelief(this, agent.Inventory[Resource.Type], false);
        }

        // Deal with supply/demand getting out of control
        if (TimeUtil.Rounds - LastRoundTraded > 5 || TimeUtil.Rounds == 1)
        {
            if (DemandThisRound() > 0)
            {
                if ((DemandThisRound() - SupplyThisRound()) / DemandThisRound() * 100 >= 75) // check to see how much more demand there is than supply
                {
                    Town.SpawnAgent(Resource.ProducedBy);
                    Console.WriteLine(String.Format("Round: {0}, Town: {1}; Market: {2}; Supply: {3}, Demand: {4}", TimeUtil.Rounds, Town.Name, Resource.DisplayName, Supply, Demand));
                }
            }
            
        }

    }

    public void HoldTownAuctions()
    {

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

    public float RecentDemandAverage()
    {
        float numberOfRounds = MarketHistory.Count >= 15 ? 15 : MarketHistory.Count;

        float demand = (float)Math.Round((MarketHistory.Sum(x => x.Demand) / numberOfRounds), 2);

        return demand;
    }

    public float RecentSupplyAverage()
    {
        float numberOfRounds = MarketHistory.Count >= 15 ? 15 : MarketHistory.Count;

        float supply = (float)Math.Round((MarketHistory.Sum(x => x.Supply) / numberOfRounds), 2);

        return supply;
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

    private void AddTransactionRecord(TransactionRecord record)
    {
        TransactionHistory.Add(record);
        if (TransactionHistory.Count > _transactionHistoryLimit)
        {
            TransactionHistory.RemoveAt(0);
        }
    }

    public List<TransactionRecord> GetAllTransactionsByBuyerType(AgentEntity.EntityType type) //These can also be moved to Town class
    {
        return TransactionHistory.Where(record => record.Buyer.Type == type).ToList();
    }

    public List<TransactionRecord> GetAllTransactionsBySellerType(AgentEntity.EntityType type) //These can also be moved to Town class
    {
        return TransactionHistory.Where(record => record.Seller.Type == type).ToList();
    }

    public List<TransactionRecord> GetAllTransactionsByResource(ResourceUtil.ResourceType type) //This is more relevant in the Town class
    {
        return TransactionHistory.Where(record => record.Resource.Type == type).ToList();
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

    public class TransactionRecord
    {
        public int Round { get; set; }
        public Entity Buyer { get; set; }
        public Entity Seller { get; set; }
        public float ClearingPrice { get; set; }
        public float AmountTraded { get; set; }
        public Resource Resource { get; set; }

        public TransactionRecord()
        {

        }

        public TransactionRecord(Entity buyer, Entity seller, float clearingPrice, float amountTraded, Resource resource)
        {
            Round = TimeUtil.Rounds;
            Buyer = buyer;
            Seller = seller;
            ClearingPrice = clearingPrice;
            AmountTraded = amountTraded;
            Resource = resource;
        }

        public string PrintTransactionRecord()
        {
            string transactionString = String.Format("{0} bought {1} {2} for {3} gold from {4}.", Buyer.Name, AmountTraded, Resource.DisplayName, ClearingPrice, Seller.Name);
            return transactionString;
        }
    }

}