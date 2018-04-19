using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource
{

    private string _consumedBy;
    private const float _trivialToProduce = 0.00f;
    private const float _easyToProduce = 0.25f;
    private const float _mediumToProduce = 0.50f;
    private const float _hardToProduce = 0.75f;
    private float _productionDifficultyModifier;

    public enum ProductionDifficulty { trivial, easy, medium, hard }

    public int Id { get; set; }
    public string DisplayName { get; set; }
    public ResourceUtil.ResourceType Type { get; set; }
    public Resource RawResource { get; set; }
    public float AmountProduced { get; set; } = 5;
    public float BasePrice { get; set; }
    public AgentEntity.EntityType ProducedBy { get; private set; }
    public string CrafterType
    {
        get { return _consumedBy; }
        private set
        {
            _consumedBy = value;
        }
    }
    public float ProductionDifficultyModifier { get; set; }

    public Resource()
    {

    }

    public Resource(int id, string displayName, ResourceUtil.ResourceType type, float basePrice, AgentEntity.EntityType producedBy, ProductionDifficulty difficultyToProduce)
    {
        Id = id;
        DisplayName = displayName;
        Type = type;
        BasePrice = basePrice;
        ProducedBy = producedBy;

        ProductionDifficultyModifier = DifficultyToProduce(difficultyToProduce);
    }


    private float DifficultyToProduce(ProductionDifficulty difficultyToProduce)
    {
        float productionDifficultyModifier = 0;
        switch (difficultyToProduce)
        {
            case ProductionDifficulty.trivial:
                productionDifficultyModifier = _trivialToProduce;
                break;
            case ProductionDifficulty.easy:
                productionDifficultyModifier = _easyToProduce;
                break;
            case ProductionDifficulty.medium:
                productionDifficultyModifier = _mediumToProduce;
                break;
            case ProductionDifficulty.hard:
                productionDifficultyModifier = _hardToProduce;
                break;
        }

        return productionDifficultyModifier;

    }
}