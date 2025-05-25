using UnityEngine;
using System;

public enum CardActionType { Radius, Line, Shape, Single, Cast} // Radius = circle/cone
public enum CardCastOrigin { Self, Target, Position }
public enum CardTargetType { All, Enemy, Ally }

[Serializable]
public struct Cost {
    public string gem;
    public int amount;
}

[Serializable]
public struct CardEffect {
    public string effectType;
    public int amount;
    public string projectileId;
}

[Serializable]
public struct VisualEffect {
    public string effectId;
    public Vector2 relativeOffset;
    public Vector2 directionOffset;
    public float size;
}

[CreateAssetMenu(fileName = "GameCard", menuName = "Card")]
public class Card : ScriptableObject {

    public string id;
    public int rarity;
    public Sprite image;

    public CardActionType action;
    public CardCastOrigin origin;
    public CardTargetType target;
    public CardEffect[] effects;

    public string title;
    public string desc;

    public Cost[] costs;

    public bool comboCard;
    public bool combinationCard;

    public float radius;
    public float angle;
    public float startup;

    public VisualEffect[] vfx;

    private void OnEnable() {

    }
}