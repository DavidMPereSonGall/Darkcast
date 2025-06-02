using UnityEngine;
using System;

[Serializable]
public struct AnimationKeyframe
{
    public FakeQuaternion[] rotations;
    public Vector3[] translations;

    public AnimationKeyframe(int boneCount)
    {
        this.rotations = new FakeQuaternion[boneCount];
        this.translations = new Vector3[boneCount];
    }

}

[CreateAssetMenu(fileName = "AnimationObject", menuName = "Game/Animation")]
public class AnimationObject : ScriptableObject
{
    public AnimationKeyframe[] keyframes;
    public int[] assignedKeyframes;
    public int duration;
}