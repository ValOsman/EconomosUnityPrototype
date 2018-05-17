using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Modifier
{
    // Production Modifier
    // Name = "Forest Fire"
    // Amount = 0.5
    // ResourceType = wood
    // ModifierType = forest_fire

    public enum Mod { fertile_fields, forest_fire };

    public string Name { get; set; }
    public float Amount { get; set; }
    public ResourceUtil.ResourceType ResourceType { get; set; }
    public Mod ModifierType { get; set; }

    public Modifier()
    {

    }


}
