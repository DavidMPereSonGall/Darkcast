using UnityEngine;
using System;

[Serializable]
public enum EffectType
{
    FaceCamera,
    Directional,
    Grounded
}

[CreateAssetMenu(fileName = "GameEffect", menuName = "Game/Effect")]
public class Effect : ScriptableObject
{
    public Sprite[] sprites;
    public int frameStep;
    public EffectType effectType;
    public float size;
}