using UnityEngine;
using System;

[CreateAssetMenu(fileName = "GameEntity", menuName = "Game/Entity")]
public class GameEntity : ScriptableObject
{
    public string entityId;
    public int maxHealth;
    public float speed;
    public float radius;
    public string[] animations;
    public float modelHeight;
    public string modelId;
    public float friction;
    public string[] cardIds;
}