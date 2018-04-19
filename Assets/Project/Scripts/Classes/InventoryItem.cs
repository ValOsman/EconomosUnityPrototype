using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryItem
{
    private int _priceHistoryLimit = 15;
    private float _amount;
    private const float _attemptedTradeLimit = 10;
    private const float _offerRecordsLimit = 10000;

    public enum ActionType { buy, sell }

    public Resource Resource { get; set; }
    public ActionType Action { get; private set; }
    public float Amount
    {
        get { return _amount; }
        set
        {
            if (value + _amount > Max)
            {
                _amount = Max;
            }
            else
            {
                _amount = value;
            }
        }
    }
    public float Max { get; set; }
    public float Ideal { get; private set; }
    public bool MakeOffer { get; set; } = true;
    public PriceAmountPair Offer { get; set; }
    public float CostToProduce { get; set; }
    public PriceRange PriceRange { get; set; }
    public List<float> PriceHistory { get; set; } //Record of prices over the last 15 rounds. A 0 indicates the agent did not buy/sell the item that round.
    public List<OfferRecord> OfferHistory { get; set; } = new List<OfferRecord>();
    public float HistoricalPriceMean { get; private set; }


    public InventoryItem(Resource resource, ActionType actionType, PriceRange priceRange, float max, float ideal, float amount = 0)
    {
        Resource = resource;
        Action = actionType;
        PriceRange = priceRange;
        Max = max;
        Ideal = ideal;
        Amount = amount;
    }

    public void AddOfferRecord(OfferRecord record)
    {
        if (OfferHistory.Count == _offerRecordsLimit)
        {
            OfferHistory.RemoveAt(0);
        }
        OfferHistory.Add(record);
    }

    public void RecordCurrentOffer(bool success)
    {
        AddOfferRecord(new OfferRecord(TimeUtil.Rounds, PriceRange, Offer, success));
    }

    public float GetTradeSuccessRatio()
    {
        float successes;
        float rounds = _attemptedTradeLimit;
        //float rounds = AttemptedTrades.Count();

        successes = OfferHistory.Skip(Math.Max(0, OfferHistory.Count() - (int)_attemptedTradeLimit)).Where(record => record.Success == true).Count();

        //successes = AttemptedTrades.Where(x => x == true).Count();

        return successes / rounds;

    }

    public void UpdatePriceHistory(float price)
    {
        PriceHistory.Add(price);
        if (PriceHistory.Count > _priceHistoryLimit)
        {
            PriceHistory.RemoveAt(0);
        }
    }

    public float Surplus()
    {
        float surplus = Amount - Ideal;

        return surplus > 0 ? surplus : 0;
    }

    public float Shortage()
    {
        float shortage = Ideal - Amount;

        return shortage > 0 ? shortage : 0;
    }

    public float FreeSpace()
    {
        return Max - Amount;
    }

    public class OfferRecord
    {
        public int Round { get; set; }
        public float Max { get; set; }
        public float Min { get; set; }
        public float Amount { get; set; }
        public float Price { get; set; }
        public bool Success { get; set; }

        public OfferRecord()
        {

        }

        public OfferRecord(int round, PriceRange priceRange, PriceAmountPair offer, bool offerAccepted)
        {
            Round = round;
            Max = priceRange.Max;
            Min = priceRange.Min;
            Amount = offer.Amount;
            Price = offer.Price;
            Success = offerAccepted;

        }
    }
}
