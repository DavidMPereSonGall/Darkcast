using UnityEngine;

[CreateAssetMenu(fileName = "GameEntity", menuName = "Game/Entity")]
public class GameEntity : ScriptableObject
{
    public string entityId;
    public int maxHealth;
    public float speed;
    public float radius;
}