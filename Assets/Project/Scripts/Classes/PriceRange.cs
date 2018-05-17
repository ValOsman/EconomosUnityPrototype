using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriceRange
{
    private float _min;
    private float _max;
    private float _mean;
    private float _meanPositionInRange;

    public float Min
    {
        get { return _min; }

        set
        {
            if (value <= 0)
            {
                _min = 1;
            }
            else if (value <= _max)
            {
                _min = value;
            }
            else if (value > _max)
            {
                _min = value;
                _max = value;
            }
            UpdateMean();
        }
    }
    public float Max
    {
        get { return _max; }
        set
        {
            if (value >= _min)
            {
                _max = value;
            }
            else if (value < _min)
            {
                _max = _min;
            }

            UpdateMean();
        }
    }

    public float Mean { get; private set; }

    public float MeanPositionInRange { get; private set; }

    public PriceRange(float min, float max)
    {
        _min = min > 0 ? min : 1; // Minimum value must be at least 1
        _max = max;
        UpdateMean();
    }

    private void UpdateMean()
    {
        Mean = (float)Math.Round((Max + Min) / 2f);
        MeanPositionInRange = FindMeanPositionInRange();
    }

    private float FindMeanPositionInRange()
    {
        return Mean - Min + 1;
    }

    public void Shrink(float magnitude)
    {
        ShrinkMax(magnitude);
        ShrinkMin(magnitude);
    }

    public void ShrinkMax(float magnitude)
    {
        Max = Max - (float)Math.Round(Max * magnitude);
    }

    public void ShrinkMin(float magnitude)
    {
        Min = Min + (float)Math.Round(Min * magnitude);
    }

    public void Grow(float magnitude)
    {
        GrowMax(magnitude);
        GrowMin(magnitude);
    }

    public void GrowMax(float magnitude)
    {
        Max = Max + (float)Math.Round(Max * magnitude);
    }

    public void GrowMin(float magnitude)
    {
        Min = Min - (float)Math.Round(Min * magnitude);
    }

    // <param name="interval">The distance between the current mean and another mean.</param>
    // <param name="magnitude">How close the current mean should translate to the new mean. Ranges from 0 to 1.</param>
    public void Translate(float interval, float magnitude = 0.5f)
    {
        //Debug info
        float oldMax = Max;
        float oldMin = Min;

        Max = (float)Math.Round(Max + interval * magnitude);
        if (Min == 1)
        {
            Max = (float)Math.Round(Max + interval * magnitude);
        }
        else
        {
            Min = (float)Math.Round(Min + interval * magnitude);
        }
        
    }

    public bool InRange(float number)
    {
        if ((Min < number && number < Max) || (Min == number) || number == Max)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public float FindPositionInRange(float num)
    {
        float count = Max - Min + 1;
        List<int> range = Enumerable.Range((int)Min, (int)count).ToList();

        float position = (float)range.FindIndex(x => x == num) + 1; // add 1 because zero-index

        return position;
    }
}