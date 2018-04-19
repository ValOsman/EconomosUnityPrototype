using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriceAmountPair
{
    private float _totalPrice;

    public float Price { get; set; }
    public float Amount { get; set; }
    public float TotalPrice
    {
        get
        {
            return Price * Amount;
        }
    }


    public PriceAmountPair(float price, float amount)
    {
        Price = price;
        Amount = amount;
    }
}
