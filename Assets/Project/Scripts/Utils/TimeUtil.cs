using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class TimeUtil : MonoBehaviour
{

    private const int _yearLength = 180;
    private const int _monthLength = 30;
    private const int _weekLength = 10;

    public static int Rounds { get; private set; } = 1;
    public static int Day { get; set; } = 1;
    public static int Week { get; set; } = 1;
    public static int Month { get; set; } = 1;
    public static int Year { get; set; } = 1000;

    public static void IncrementRound()
    {
        Rounds++;
            
        if(Month * _monthLength == _yearLength)
        {
            Year++;
            Month = 1;
            Week = 1;
        }
        else if(Day == _monthLength)
        {
            Month = (Month == (_yearLength / _monthLength)) ? 1 : (Month + 1);
            Week = 1;
        }
        else if(Day == _weekLength || Day == _weekLength * 2)
        {
            Week++;
        }

        Day = (Day == 30) ? 1 : (Day + 1);

    }

    public static bool StartOfMonth()
    {
        return (Week == 1) ? true : false;
    }

    public static bool MiddleOfMonth()
    {
        return (Week == 2) ? true : false;
    }

    public static bool EndOfMonth()
    {
        return (Week == 3) ? true : false;
    }

    public static string GetDay()
    {
        return Ordinal(Day);
    }

    public static string GetMonth()
    {
        switch(Month)
        {
            case 1:
                return "Justice";
            case 2:
                return "Faith";
            case 3:
                return "Modesty";
            case 4:
                return "Ask";
            case 5:
                return "Bid";
            case 6:
                return "Coin";
            default:
                return "";
        }
    }

    private static string Ordinal(int number)
    {
        const string TH = "th";
        var s = number.ToString();

        number %= 100;

        if ((number >= 11) && (number <= 13))
        {
            return s + TH;
        }

        switch (number % 10)
        {
            case 1:
                return s + "st";
            case 2:
                return s + "nd";
            case 3:
                return s + "rd";
            default:
                return s + TH;
        }
    }
}

