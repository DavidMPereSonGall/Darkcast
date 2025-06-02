using UnityEngine;
using System;

public enum CardCastOrigin { Self, Target, Position }
public enum CardTargetType { All, Enemy, Ally }

[Serializable]
public struct VisualEffect {
    public string effectId;
    public Vector2 worldOffset;
    public Vector2 localOffset;
    public bool onSelf;
    public Quaternion rotation;
    public string boneId;
}

[Serializable]
public struct CardCollider
{
    public int duration;
    public Vector2[] positions;
    public float[] radius;
    public float[] knockback;
    public int[] damage;
    public Vector2[] knockbackDirection;
    public string[] effects;
    public int stun;
}

[Serializable]
public class CardAction
{
    public int delay;
    public string swapItemId;
    public string animationId;
    public string substitutionId;
    public bool faceTarget;
    public CardCollider[] cardColliders;
    public VisualEffect[] visualEffects;
    public Vector2 applyVelocity;
    public string soundId;

    public CardAction Clone()
    {
        var newCardAction = new CardAction();
        newCardAction.delay = this.delay;
        newCardAction.swapItemId = this.swapItemId;
        newCardAction.animationId = this.animationId;
        newCardAction.substitutionId = this.substitutionId;
        newCardAction.faceTarget = this.faceTarget;
        newCardAction.cardColliders = this.cardColliders;
        newCardAction.visualEffects = this.visualEffects;
        newCardAction.applyVelocity = this.applyVelocity;
        newCardAction.soundId = this.soundId;
        return newCardAction;
    }
}

[CreateAssetMenu(fileName = "GameCard", menuName = "Game/Card")]
public class Card : ScriptableObject 
{
    public Sprite image;

    public CardAction[] actions;
    public CardCastOrigin origin;
    public CardTargetType target;

    public string title;
    public string desc;

    public bool comboCard;
    public bool combinationCard;

    public int delay;
}