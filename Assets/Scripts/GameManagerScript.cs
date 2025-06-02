using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using System.Globalization;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public enum TargetMode
{
    TowardsTarget,
    TowardsPosition,
    TowardsDirection,
    AwayTarget,
    None
}

public enum MoveMode
{
    MoveVelocity,
    MovePath
}

[Serializable]
public struct FakeQuaternion
{
    public float w;
    public float x;
    public float y;
    public float z;
    
    public FakeQuaternion(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }
}

public class SpriteEffect
{
    public Effect effectInfo;
    public int currentFrame;
    public int elapsedFrames;
    public int frameStep;
    public int animationFrames;
    public int duration;
    public int totalPlayedFrames;
    public GameObject obj;
    public SpriteRenderer spriteRenderer;

    public SpriteEffect(Effect effectInfo, GameObject obj, int duration)
    {
        this.effectInfo = effectInfo;
        this.obj = obj;
        this.frameStep = effectInfo.frameStep;
        this.spriteRenderer = obj.GetComponent<SpriteRenderer>();
        this.animationFrames = effectInfo.sprites.Length;
        this.duration = duration;
    }

    public SpriteEffect(Effect effectInfo, GameObject obj)
    {
        this.effectInfo = effectInfo;
        this.obj = obj;
        this.frameStep = effectInfo.frameStep;
        this.spriteRenderer = obj.GetComponent<SpriteRenderer>();
        this.animationFrames = effectInfo.sprites.Length;
        this.duration = animationFrames * frameStep;
    }

    public void Step()
    {
        if (elapsedFrames >= frameStep) {
            currentFrame++;
            elapsedFrames = 0;
        }

        if (currentFrame < animationFrames)
        {
            spriteRenderer.sprite = effectInfo.sprites[currentFrame];
        } else
        {
            currentFrame = 0;
        }

        elapsedFrames++;
        totalPlayedFrames++;
    }
}

public class EntityAnimation
{
    public AnimationKeyframe[] keyframes;
    public int[] assignedKeyframes;
    public int duration;

    public EntityAnimation(int keyframeAmount, int duration) {
        this.keyframes = new AnimationKeyframe[keyframeAmount];
        this.assignedKeyframes = new int[duration];
        this.duration = duration;
    }

    public EntityAnimation(AnimationKeyframe[] keyframes, int duration)
    {
        this.keyframes = keyframes;
        this.assignedKeyframes = new int[duration];
        this.duration = duration;
    }

    public EntityAnimation(AnimationObject animationObject)
    {
        this.keyframes = animationObject.keyframes;
        this.assignedKeyframes = animationObject.assignedKeyframes;
        this.duration = animationObject.duration;
    }
}

public class EntityAnimator
{
    public Dictionary<string, EntityAnimation> animations;
    
    public Transform[] rigBones;
    public Quaternion[] originalRotations;
    public Vector3[] originalTranslations;
    public Transform armature;

    public string lastPlayingAnimation;
    public string playingAnimation;
    public string stateAnimation;
    public int currentFrame;
    public int activeAnimationFrame;
    public int animatorState;
    // 0 idle
    // 1 walk
    // 2 run

    public EntityAnimator(Transform armature)
    {
        this.armature = armature;
        this.rigBones = armature.GetComponentsInChildren<Transform>()
                             .Where(t => t != armature)
                             .OrderBy(b => b.name)
                             .ToArray();
        this.originalRotations = rigBones.Select(b => b.localRotation).ToArray();
        this.originalTranslations = rigBones.Select(b => b.localPosition).ToArray();
        this.animations = new Dictionary<string, EntityAnimation>();
        currentFrame = -1;
        activeAnimationFrame = -1;
        playingAnimation = "idle";
        stateAnimation = "idle";
    }

    public void PlayHit()
    {
        if (playingAnimation == "idle" || playingAnimation == "walk")
        {
            ForcePlayAnimation("hit");
        }
    }

    public void SetState(int state)
    {
        if (animatorState == state) return;

        animatorState = state;
        if (state == 0) PlayStateAnimation("idle");
        if (state == 1) PlayStateAnimation("walk");
        if (state == 2) PlayStateAnimation("run");
    }

    public void UpdateBones()
    {
        var animation = animations[playingAnimation];
        var rotations = animation.keyframes[activeAnimationFrame].rotations;
        var translations = animation.keyframes[activeAnimationFrame].translations;

        for (int boneId = 0; boneId < rigBones.Length; boneId++)
        {
            var fakeQuat = rotations[boneId];
            rigBones[boneId].localRotation = originalRotations[boneId] * new Quaternion(fakeQuat.x, fakeQuat.y, fakeQuat.z, fakeQuat.w);
            rigBones[boneId].localPosition = originalTranslations[boneId] + originalRotations[boneId] * translations[boneId]; //+ translations[boneId];
        }
    }

    public void Step()
    {
        var animation = animations[playingAnimation];
        var duration = animation.duration;

        currentFrame += 1;

        if (currentFrame >= duration-1)
        {
            currentFrame = 0;

            if (playingAnimation != stateAnimation)
            {
                ForcePlayAnimation(stateAnimation);
                return;
            }
        }

        var keyframe = animation.assignedKeyframes[currentFrame];

        if (keyframe != activeAnimationFrame  || playingAnimation != lastPlayingAnimation)
        {
            activeAnimationFrame = keyframe;
            lastPlayingAnimation = playingAnimation;
            UpdateBones();
        }
    }

    public void ForcePlayStateAnimation(string animationId)
    {
        playingAnimation = animationId;
        stateAnimation = animationId;
        currentFrame = 0;
        activeAnimationFrame = 0;
        //UpdateBones();
    }

    public void PlayStateAnimation(string animationId)
    {
        if (playingAnimation != animationId)
        {
            playingAnimation = animationId;
            stateAnimation = animationId;
            currentFrame = 0;
            activeAnimationFrame = 0;
        }
    }

    public void PlayAnimation(string animationId)
    {
        if (playingAnimation != animationId)
        {
            playingAnimation = animationId;
            currentFrame = 0;
            activeAnimationFrame = 0;
        }
    }

    public void ForcePlayAnimation(string animationId)
    {
        playingAnimation = animationId;
        currentFrame = 0;
        activeAnimationFrame = 0;
    }

    public void AddAnimation(string animationId, EntityAnimation animation)
    {
        animations[animationId] = animation;
    }

    public void AddAnimation(string animationId, AnimationObject animationObj)
    {
        var keyframeAmount = animationObj.keyframes.Length;
        var duration = animationObj.duration;
        var animation = new EntityAnimation(keyframeAmount, duration);

        animation.assignedKeyframes = animationObj.assignedKeyframes;
        animation.keyframes = animationObj.keyframes;

        animations[animationId] = animation;
    }
}

public struct EntityCardAction
{
    public Card card;
    public string targetUuid;
    public Vector2 targetDirection;
    public Vector2 targetPosition;
}

public class CardContainer
{
    public GameObject cardObject;
    public Image backImage;
    public float rotationSpeed;
    public Quaternion rotation;
    public float extraRotation;
    public bool spinning;
    public Vector3 position;
    public Vector3 targetPosition;
    public Card card;
    
    public CardContainer(GameObject cardObject, Image backImage, Card card)
    {
        this.backImage = backImage;
        this.cardObject = cardObject;
        this.card = card;
        spinning = false;
    }

    public void Spin()
    {
        extraRotation = 1f;
    }

    public void UpdateRotation(float delta)
    {
        var rot = Quaternion.Euler(rotation.x, rotation.y + extraRotation*360f, rotation.z);
        if (extraRotation > 0f) extraRotation = Mathf.Clamp01(extraRotation-delta*2.3f);
        var rotForward = rot * Vector3.forward;
        var angle = Vector3.Angle(rotForward, Vector3.forward);
        if (angle > 90f && !backImage.enabled)
        {
            backImage.enabled = true;
        }
        else if (angle < 90f && backImage.enabled)
        {
            backImage.enabled = false;
        }
        rotation = rot;
    }

    public void UpdatePosition(float delta)
    {
        var diff = (targetPosition - position);
        var distance = diff.magnitude;
        var speed = diff.normalized * delta * 10f * distance;
        position += speed;
    }

    public void UpdateCoordinates(float delta)
    {
        UpdateRotation(delta);
        UpdatePosition(delta);
        cardObject.transform.localRotation = rotation;
        cardObject.transform.localPosition = position;
    }
}

public class EnemyAIHandler
{
    public List<Vector2> endPositions;
    public Dictionary<string, Entity> gameEntities;

    public EnemyAIHandler(Dictionary<string, Entity> gameEntities)
    {
        this.gameEntities = gameEntities;
        this.endPositions = new List<Vector2>();
    }

    public void CalculateEntityTurn(Entity entity)
    {
        var targetEntity = gameEntities[entity.targetUuid];
        var targetPosition = targetEntity.position;
        var targetToEntity = (entity.position - targetPosition);
        var destination = targetPosition + targetToEntity.normalized * entity.radius * 2f;

        foreach (var otherPosition in endPositions)
        {
            var diff = (otherPosition - destination);
            var sqrMagnitude = diff.sqrMagnitude;
            var clamp = Mathf.Clamp(sqrMagnitude, 0.1f,6);
            destination -= diff.normalized * (1f/clamp);
        }

        endPositions.Add(destination);
        var entityPath = DrawPathToDestination(entity, destination, Mathf.Min(entity.speed * 2f, (entity.position - destination).magnitude));

        entity.movePath = entityPath;

        if (entityPath.Count < Mathf.Floor(entity.speed * 1.8f))
        {
            var deckCount = entity.deck.cards.Count;

            if (deckCount > 0)
            {
                var rand = UnityEngine.Random.Range(0, deckCount);
                entity.QueueCard(entity.deck.cards[rand], entity.targetUuid);
            }
        }
    }

    public Path DrawPathToDestination(Entity entity, Vector2 destination, float travelLength)
    {
        var path = new Path();
        var lastPos = entity.position;
        var dir = (destination - lastPos).normalized;
        var drawDistance = entity.speed * 1f / 4f;

        for (var i = 0; i <= 10; i++)
        {
            var newPos = lastPos + dir * drawDistance;
            travelLength -= drawDistance;
            path.Add(newPos);
            if (travelLength <= 0) break;
            lastPos = newPos;
        }

        return path;
    }

    public void ClearEndPositions()
    {
        endPositions.Clear();
    }
}

public class DeckUIHandler
{
    public Deck deck;
    public List<CardContainer> cardContainers;
    public int selectedId;
    public int hoverId;

    public DeckUIHandler(Deck deck)
    {
        this.deck = deck;
        this.cardContainers = new List<CardContainer>();
        this.selectedId = -1;
        this.hoverId = -1;
    }

    public void PlayCard(Entity entity)
    {

    }

    public void AddCardContainer(CardContainer cardContainer)
    {
        deck.AddCard(cardContainer.card);
        cardContainer.targetPosition = new Vector3(0, 0, 0);
        cardContainers.Add(cardContainer);
    }

    public void DiscardAll()
    {
        for (int i = cardContainers.Count-1; i >= 0; i--)
        {
            DiscardCard(i);
        }
    }

    public void DiscardCard(int id)
    {
        deck.discardPile.Add(deck.cards[id]);
        deck.cards.RemoveAt(id);
        GameManagerScript.Destroy(cardContainers[id].cardObject);
        cardContainers.RemoveAt(id);
        if (id == selectedId) selectedId = -1;
        if (selectedId > id) selectedId--;
    }

    public void SelectCard(float xPos)
    {
        var count = cardContainers.Count;
        var total = (count == 1) ? 100 : count * 150;
        var half = total / 2;

        var rel = (xPos + half) / total;
        var round = Mathf.RoundToInt(rel * (count - 1));

        if (round == selectedId)
        {
            selectedId = -1;
            return;
        }

        if (round > -1 && round < count)
        {
            selectedId = round;
            cardContainers[selectedId].Spin();
        }
        else
        {
            if (rel == -1)
            {
                selectedId = 0;
                cardContainers[selectedId].Spin();
                return;
            }
            if (rel == count)
            {
                selectedId = count - 1;
                cardContainers[selectedId].Spin();
                return;
            }
            selectedId = -1;
        }
    }

    public void HoverCard(float xPos)
    {
        var count = cardContainers.Count;
        var total = (count == 1) ? 0 : count * 150;
        var half = total / 2;

        var rel = (xPos + half) / total;
        var round = Mathf.RoundToInt(rel * (count-1));

        if (round > -1 && round < count)
        {
            hoverId = round;
        } else { 
            if (rel == -1)
            {
                selectedId = 0;
                return;
            }
            if (rel == count)
            {
                selectedId = count - 1;
                return;
            } 
            hoverId = -1;
        }

    }

    public void UpdateDeck(float delta)
    {
        var count = cardContainers.Count;
        var total = (count==1) ? 0 : count * 150;
        var half = total/2;

        for (int i = 0; i < count; i++)
        {
            var t = (count == 1) ? 0 : (float) i /(float) (count-1);
            var x = total * t - half;
            var y = (i == hoverId) ? -525 : -550;
            y = (i == selectedId) ? -450 : y;
            var pos = new Vector3(x, y, 0);
            cardContainers[i].targetPosition = pos;
            cardContainers[i].UpdateCoordinates(delta);
        }
    }
}

public class Deck
{
    public List<Card> cards;
    public List<Card> activeHand;
    public List<Card> discardPile;

    public Deck()
    {
        cards = new List<Card>();
        cards = new List<Card>();
        activeHand = new List<Card>();
        discardPile = new List<Card>();
    }

    public void AddCard(Card card)
    {
        cards.Add(card);
    }

    public void ShuffleDiscards()
    {
        System.Random rng = new System.Random();
        int n = discardPile.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            // Swap
            Card temp = discardPile[k];
            discardPile[k] = discardPile[n];
            discardPile[n] = temp;
        }
    }

    public void ShuffleCards()
    {
        System.Random rng = new System.Random();
        int n = cards.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            // Swap
            Card temp = cards[k];
            cards[k] = cards[n];
            cards[n] = temp;
        }
    }

    public void MoveDiscardPile()
    {
        for (int j = 0; j < discardPile.Count; j++)
        {
            cards.Add(discardPile[j]);
        }

        discardPile.Clear();
    }

    public void DrawFirstCard()
    {
        activeHand.Add(cards[0]);
        cards.RemoveAt(0);
    }

    public void Draw(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (cards.Count > 0)
            {
                DrawFirstCard();
                continue;
            }
            ShuffleDiscards();
            MoveDiscardPile();
            DrawFirstCard();
        }
    }
}

public class Entity
{
    public GameObject gameObject;
    public string uniqueId;
    public string nameId;
    public Vector2 position;
    public float modelHeight;
    public float rotation;
    public float targetRotation;
    public int team;

    public GameObject entityCircle;
    public Color circleColor;

    public GameObject itemObject;
    public string itemId;

    public EntityAnimator animator;

    public int maxHealth;
    public float speed;
    public float acceleration;
    public float radius;
    public float moveSpeed;

    public int health;
    public int stun;

    public Vector2 moveVelocity;
    public float friction;
    public Path movePath;
    
    public string targetUuid;
    public Vector2 targetPosition;
    public Vector2 targetDirection;

    public MoveMode moveMode;
    public TargetMode targetMode;

    public int actionDelay;
    public List<CardAction> actionQueue;
    public List<EntityCardAction> cardQueue;
    public Deck deck;

    public Entity(string uniqueId, string nameId, float xPos, float yPos, int team, GameObject obj, GameObject entityCircle)
    {
        this.uniqueId = uniqueId;
        this.nameId = nameId;
        this.gameObject = obj;
        this.entityCircle = entityCircle;
        this.position = new Vector2(xPos, yPos);
        this.rotation = 0;
        this.team = team;
        this.movePath = new Path();
        this.deck = new Deck();
        this.actionQueue = new List<CardAction>();
        this.cardQueue = new List<EntityCardAction>();
        this.actionDelay = 0;

    var armature = obj.transform.Find("Armature");
        this.animator = new EntityAnimator(armature);
    }

    public Entity(string uniqueId, string nameId, Vector2 position, int team, GameObject obj)
    {
        this.uniqueId = uniqueId;
        this.nameId = nameId;
        this.gameObject = obj;
        this.position = position;
        this.rotation = 0f;
        this.team = team;
    }

    public void ClearStun()
    {
        this.stun = 0;
    }

    public void RemoveStun(int amount)
    {
        this.stun -= amount;

        if (this.stun < 1)
        {
            this.stun = 0;
        }
    }

    public void SetStun(int amount)
    {
        if (amount > this.stun) this.stun = amount;
        moveMode = MoveMode.MoveVelocity;
    }

    public void HitWithCollision(PathCollisionResult collision)
    {
        this.moveVelocity = collision.knockback * collision.knockbackDirection;
        this.movePath.Clear();
        HitAndDamage(collision.damage, collision.stun);
    }

    public void HitAndDamage(int damage, int stun)
    {
        this.health -= damage;
        this.animator.PlayHit();
        SetStun(stun);
    }

    public void ForceMoveTo(Vector2 position, float rotation)
    {
        this.position = position;
        this.rotation = rotation;
        this.entityCircle.transform.position = new Vector3(position.x, 0.1f, position.y);
        this.gameObject.transform.position = new Vector3(position.x, modelHeight, position.y);
        this.gameObject.transform.rotation = Quaternion.Euler(0f, rotation, 0f) * Quaternion.Euler(0f, 90f, 0f);
    }

    public void UpdateRotation(float deltaTime)
    {
        this.rotation = Mathf.MoveTowardsAngle(rotation, targetRotation, (180f + Mathf.Abs(Mathf.DeltaAngle(targetRotation, rotation))) * deltaTime);
        this.gameObject.transform.rotation = Quaternion.Euler(0f, rotation, 0f) * Quaternion.Euler(0f, 90f, 0f);
    }

    public void MoveTo(Vector2 position, float targetRotation)
    {
        this.position = position;
        this.targetRotation = targetRotation;
        this.entityCircle.transform.position = new Vector3(position.x, 0.1f, position.y);
        this.gameObject.transform.position = new Vector3(position.x,modelHeight,position.y);
        //this.gameObject.transform.rotation = Quaternion.Euler(0f, rotation, 0f) * Quaternion.Euler(0f,90f,0f);
    }

    public void MoveTo(float xPos, float yPos, float targetRotation)
    {
        this.position = new Vector2(xPos, yPos);
        this.targetRotation = targetRotation;
        this.entityCircle.transform.position = new Vector3(xPos, 0.1f, yPos);
        this.gameObject.transform.position = new Vector3(xPos, modelHeight, yPos);
        //this.gameObject.transform.rotation = Quaternion.Euler(0f,rotation,0f) * Quaternion.Euler(0f, 90f, 0f);
    }

    public void MoveTo(Vector2 position)
    {
        this.position = position;
        this.entityCircle.transform.position = new Vector3(position.x, 0.1f, position.y);
        this.gameObject.transform.position = new Vector3(position.x, modelHeight, position.y);
    }

    public void MoveTo(float xPos, float yPos)
    {
        this.position = new Vector2(xPos, yPos);
        this.gameObject.transform.position = new Vector3(xPos, modelHeight, yPos);
    }

    public void AddMovePoint(Vector2 position)
    {
        movePath.Add(position);
    }

    public CardAction[] CloneCardActionArray(CardAction[] cardActionArray)
    {
        var newCardActionArray = new CardAction[cardActionArray.Length];
        for (int i = 0; i < cardActionArray.Length; i++)
        {
            newCardActionArray[i] = cardActionArray[i].Clone();
        }
        return newCardActionArray;
    }

    public void UpdateCardQueue()
    {
        if (this.actionQueue.Count == 0 && this.actionDelay <= 0)
        {
            if (this.cardQueue.Count > 0)
            {
                var entityCardAction = cardQueue[0];
                this.targetUuid = entityCardAction.targetUuid;
                this.targetDirection = entityCardAction.targetDirection;
                this.targetPosition = entityCardAction.targetPosition;
                var cardActions = entityCardAction.card.actions;
                var cardActionsClone = CloneCardActionArray(cardActions);
                this.actionQueue.AddRange(cardActionsClone);
                this.actionDelay = entityCardAction.card.delay;
                cardQueue.RemoveAt(0);
            }
        }
        
    }

    public void QueueCard(Card card, Vector2 targetPosition)
    {
        var cardAction = new EntityCardAction();
        cardAction.card = card;
        cardAction.targetPosition = targetPosition;
        cardQueue.Add(cardAction);
    }

    public void QueueCard(Card card, string targetUuid)
    {
        var cardAction = new EntityCardAction();
        cardAction.card = card;
        cardAction.targetUuid = targetUuid;
        cardAction.targetDirection = OperationUtils.AngleToVector2(targetRotation);
        cardQueue.Add(cardAction);
    }
}

public class Path
{
    public List<Vector2> points;
    public float maxX;
    public float maxY;
    public float minX;
    public float minY;

    public Path()
    {
        this.points = new List<Vector2>();
    }
    public Path(Path other) //Clone
    {
        this.points = new List<Vector2>(other.points);

        this.maxX = other.maxX;
        this.maxY = other.maxY;
        this.minX = other.minX;
        this.minY = other.minY;
    }

    public void Clear()
    {
        this.points.Clear();
        this.maxX = 0;
        this.maxY = 0;
        this.minX = 0;
        this.minY = 0;
    }

    public Vector2 this[int index]
    {
        get => points[index];
        set => points[index] = value;
    }

    public int Count => points.Count;

    public void CalculateBounds()
    {
        foreach (var point in points)
        {
            var x = point.x;
            var y = point.y;
            if (x > this.maxX) this.maxX = x;
            else if (x < this.minX) this.minX = x;

            if (y > this.maxY) this.maxY = y;
            else if (y < this.minY) this.minY = y;
        }
    }

    public void RemoveFirst()
    {
        var firstPoint = this.points[0];
        this.points.RemoveAt(0);

        if (firstPoint.x == maxX || firstPoint.x == minX || firstPoint.y  == minY || firstPoint.y == maxY)
        {
            CalculateBounds();
        }
    }

    public void Add(Vector2 point)
    {
        this.points.Add(point);
        var x = point.x;
        var y = point.y;

        if (this.points.Count > 1) {
            if (x > this.maxX) this.maxX = x;
            else if (x < this.minX) this.minX = x;

            if (y > this.maxY) this.maxY = y;
            else if (y <  this.minY) this.minY = y;
        }
    }

}

public struct PathCollisionResult
{
    public Vector2 entityPosition;
    public float entityRotation;
    public float traveledDistance;
    public bool hasCollided;

    public Vector2 position;
    public float knockback;
    public int damage;
    public Vector2 knockbackDirection;
    public string[] effects;
    public int stun;
}

public struct SegmentPointResult
{
    public float projection;
    public Vector2 position;

    public SegmentPointResult(Vector2 position, float projection)
    {
        this.position = position;
        this.projection = projection;
    }
}

public static class OperationUtils
{
    static float ParseFloat(string s) => float.Parse(s, CultureInfo.InvariantCulture);

    public static Vector2 PerpendicularVector(Vector2 direction)
    {
        return new Vector2(-direction.y, direction.x);
    }

    public static Vector2 OffsetPosition(Vector2 origin, Vector2 direction, Vector2 offset)
    {
        var right = new Vector2(-direction.y,direction.x);
        return (origin + direction * offset.y + right * offset.x);
    }

    public static Vector2 OffsetDirection(Vector2 direction, Vector2 amount)
    {
        var right = new Vector2(-direction.y, direction.x);
        return (direction * amount.y + right * amount.x).normalized;
    }

    public static void SpriteFaceCameraAndAim(Transform sprite, Vector2 direction, Camera cam)
    {
        sprite.forward = Camera.main.transform.forward;
        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        sprite.rotation *= Quaternion.Euler(0, 0, angle+45f);
    }

    public static Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }

    public static Vector2 AngleToVector2(float angle)
    {
        float radians = angle * Mathf.Deg2Rad;

        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    public static EntityAnimation StringToAnimation(string animationValue)
    {
        var split = animationValue.Split("/");
        var duration = int.Parse(split[0]);
        var keyframeAmount = int.Parse(split[3]);
        var bones = split[1].Split("+");
        var boneCount = bones.Length;
        var keyframesSplit = split[2].Split("?");
        //var keyframeCount = keyframesSplit.Length;

        var animation = new EntityAnimation(keyframeAmount, duration);

        var lastFrame = 0;
        for (int i = 0; i < keyframeAmount; i++)
        {
            var newSpl = keyframesSplit[i].Split("#");
            var frame = int.Parse(newSpl[0]) - 1;
            var keyframe = new AnimationKeyframe(boneCount);
            for (int boneId = 0; boneId < bones.Length; boneId++)
            {
                var boneStr = newSpl[boneId + 1];
                var boneSpl = boneStr.Split("+");
                var strQuaternion = boneSpl[0];
                var strVector = boneSpl[1];
                var newQuaternion = OperationUtils.StringToFakeQuaternion(strQuaternion);//OperationUtils.StringToQuaternion(strQuaternion, "YXZ");
                var newVector = OperationUtils.StringToVector3(strVector);
                keyframe.rotations[boneId] = newQuaternion;
                keyframe.translations[boneId] = newVector;
            }

            for (int j = lastFrame; j < frame; j++)
            {
                animation.assignedKeyframes[j] = i - 1;
            }
            lastFrame = frame;

            animation.keyframes[i] = keyframe;
        }

        for (int j = lastFrame; j < duration; j++)
        {
            animation.assignedKeyframes[j] = keyframeAmount - 1;
        }

        return animation;
    }

    public static Quaternion MirrorQuaternion(Quaternion q)
    {
        return new Quaternion(-q.x, q.y, q.z, -q.w);
    }

    public static Color DimColor(Color color)
    {
        return new Color(color.r / 2, color.g / 2, color.b / 2, 1);
    }

    public static FakeQuaternion StringToFakeQuaternion(string str)
    {
        string[] split = str.Split('_');
        var quat = new FakeQuaternion(ParseFloat(split[0]), -ParseFloat(split[1]), -ParseFloat(split[2]), ParseFloat(split[3]));
        return quat;
    }

    public static float DirectionToAngle(Vector2 direction)
    {
        return Mathf.Atan2(-direction.y, direction.x) * Mathf.Rad2Deg;
    }

    public static Quaternion StringToQuaternion(string str, string rotationOrder)
    {
        string[] split = str.Split('_');
        var quat = new Quaternion(ParseFloat(split[0]), -ParseFloat(split[2]), ParseFloat(split[1]), ParseFloat(split[3]));
        return quat;
    }

    public static Vector3 StringToVector3(string str)
    {
        string[] split = str.Split('_');
        return new Vector3(-ParseFloat(split[0])/100f, -ParseFloat(split[1])/100f, ParseFloat(split[2])/100f);
    }

    public static Vector2 GetMouseWorldPositionOrtho(Camera camera, float yPlane = 0f)
    {
        Vector3 mousePos = Input.mousePosition;

        // Convert screen position to normalized viewport (0 to 1)
        Vector2 viewportPos = new Vector2(mousePos.x / Screen.width, mousePos.y / Screen.height);

        // Get size of the orthographic view
        float camHeight = camera.orthographicSize * 2f;
        float camWidth = camHeight * camera.aspect;

        // Calculate world offsets from the center of the screen
        float offsetX = (viewportPos.x - 0.5f) * camWidth;
        float offsetY = (viewportPos.y - 0.5f) * camHeight;

        // Rotate the offset based on camera rotation (XY plane)
        Vector3 right = camera.transform.right;
        Vector3 up = camera.transform.up;

        // Project from camera position using offset
        Vector3 worldPos = camera.transform.position + (right * offsetX) + (up * offsetY);

        // Project onto a plane at y = yPlane if camera isn't perfectly top-down
        if (Mathf.Abs(camera.transform.forward.y) > 0.0001f)
        {
            float dy = worldPos.y - yPlane;
            float forwardY = camera.transform.forward.y;

            float t = dy / forwardY;
            worldPos -= camera.transform.forward * t;
            worldPos.y = yPlane;
        }

        return new Vector2(worldPos.x,worldPos.z);
    }
    /*
    public static Vector2? GetMousePos()
    {
        Vector3 mousePos = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero); // y = 0 plane

        float distance;
        if (groundPlane.Raycast(ray, out distance))
        {
            Vector3 worldPosition = ray.GetPoint(distance);

            Vector2 hitPos = new Vector2(worldPosition.x, worldPosition.z);
            return hitPos;
        }

        return null;
    }
    public static Path GeneratePath()
    {

    }
     */

    public static float DistanceFromPointToLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        float dx = lineEnd.x - lineStart.x;
        float dy = lineEnd.y - lineStart.y;
        return Mathf.Abs(dy * point.x - dx * point.y + lineEnd.x * lineStart.y - lineEnd.y * lineStart.x) / Mathf.Sqrt(dy * dy + dx * dx);
    }

    public static SegmentPointResult ClosestPointOnSegment(Vector2 startPos, Vector2 endPos, Vector2 point)
    {
        var diff = endPos - startPos;
        var mag = diff.magnitude;
        float proj = Mathf.Clamp(Vector2.Dot(point - startPos, diff),0,mag);
        if (mag == 0) { }
        float t = proj / mag;        
        return new SegmentPointResult(startPos + t * diff, proj);
    }

    public static bool DoesEntityCollide(Vector2 entityPos, float entityRadius, HitCollider hitCollider)
    {
        for (int i = 0; i < hitCollider.positions.Length; i++)
        {
            var colliderPos = hitCollider.positions[i];
            var colliderRadius = hitCollider.radius[i];
            var distance = (entityPos-colliderPos).magnitude;
            if (distance <= (colliderRadius + entityRadius)) return true;
        }

        return false;
    }

    public static PathCollisionResult EntityRadiusCollision(Vector2 entityPos, float entityRadius, HitCollider hitCollider)
    {
        float shortestDistance = 1e6f;
        var collided = false;
        int closestNumerator = 0;

        var collisionResult = new PathCollisionResult();

        for (int i = 0; i < hitCollider.positions.Length; i++)
        {
            var colliderPos = hitCollider.positions[i];
            var colliderRadius = hitCollider.radius[i];
            var distance = (entityPos - colliderPos).magnitude;
            if (distance <= (colliderRadius + entityRadius) && distance < shortestDistance)
            {
                if (!collided) collided = true;
                shortestDistance = distance;
                collisionResult.entityPosition = entityPos;
                closestNumerator = i;
            }
        }

        collisionResult.hasCollided = collided;

        if (collided)
        {
            collisionResult.position = hitCollider.positions[closestNumerator];
            collisionResult.damage = hitCollider.damage[closestNumerator];
            collisionResult.knockback = hitCollider.knockback[closestNumerator];
            collisionResult.effects = hitCollider.effects;
            collisionResult.knockbackDirection = hitCollider.knockbackDirection[closestNumerator];
            collisionResult.stun = hitCollider.stun;
        }

        return collisionResult;
    }

    public static PathCollisionResult EntitySegmentCollision(Vector2 startPos, Vector2 endPos, float entityRadius, HitCollider hitCollider)
    {
        float shortestDistance = 1e6f;
        var collided = false;
        int closestNumerator = 0;

        var collisionResult = new PathCollisionResult();

        for (int i = 0; i < hitCollider.positions.Length; i++)
        {
            var colliderPos = hitCollider.positions[i];
            var colliderRadius = hitCollider.radius[i];
            var segmentResult = ClosestPointOnSegment(startPos, endPos, colliderPos);
            var distance = (colliderPos - segmentResult.position).magnitude;
            //var distance = DistanceFromPointToLine(colliderPos,startPos,endPos);
            if (distance <= (colliderRadius + entityRadius) && distance < shortestDistance)
            {
                if (!collided) collided = true;
                shortestDistance = distance;
                //var segmentResult = ClosestPointOnSegment(startPos, endPos, colliderPos);
                collisionResult.entityPosition = segmentResult.position;
                collisionResult.entityRotation = DirectionToAngle(endPos - startPos);
                collisionResult.traveledDistance = segmentResult.projection;
                closestNumerator = i;
            }
        }

        collisionResult.hasCollided = collided;

        if (collided)
        {
            collisionResult.position = hitCollider.positions[closestNumerator];
            collisionResult.damage = hitCollider.damage[closestNumerator];
            collisionResult.knockback = hitCollider.knockback[closestNumerator];
            collisionResult.effects = hitCollider.effects;
            collisionResult.knockbackDirection = hitCollider.knockbackDirection[closestNumerator];
            collisionResult.stun = hitCollider.stun;
        }

        return collisionResult;
    }

    public static PathCollisionResult EntityPathCollision(Entity entity, Path motionPath, HitCollider hitCollider)
    {
        var lastPosition = entity.position;
        var lastRotation = entity.rotation;

        var entityRadius = entity.radius;

        for (int i = 0; i < motionPath.Count; i++)
        {
            var nextPosition = motionPath[i];
            var collisionResult = EntitySegmentCollision(lastPosition, nextPosition, entityRadius, hitCollider);
            if (collisionResult.hasCollided) return collisionResult;
            lastPosition = nextPosition;
        }

        var result = new PathCollisionResult();
        result.hasCollided = false;
        result.entityPosition = lastPosition;

        return result;
    }

    public static bool DoBoundsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        Vector2 aMin = Vector2.Min(a1, a2);
        Vector2 aMax = Vector2.Max(a1, a2);

        Vector2 bMin = Vector2.Min(b1, b2);
        Vector2 bMax = Vector2.Max(b1, b2);

        bool noOverlap =
            aMax.x < bMin.x || aMin.x > bMax.x ||
            aMax.y < bMin.y || aMin.y > bMax.y;

        return !noOverlap;
    }

    public static void TwoLineDistance(Vector2 start1, Vector2 end1, Vector2 start2, Vector2 end2, float radius1, float radius2)
    {
        var desiredDistance = radius1 + radius2;


    }

    public static PathCollisionResult[] PathOnPathDetection(Path path1, Path path2, float radius1, float radius2) { 
    
        for (int i = 1; i < path1.Count; i++)
        {
            var firstPoint = path1[i-1];
            var secondPoint = path1[i];

            var diff1 = (firstPoint - secondPoint);
            var length1 = diff1.magnitude;


        }

        return new PathCollisionResult[2];

    }

    public static Vector2 PositionAlongPath(Vector2 lastPosition, Path movePath, float travelDistance)
    {
        for (int i = 0; i < movePath.Count; i++)
        {
            var nextPosition = movePath[i];
            var diff = nextPosition - lastPosition;
            var distance = diff.magnitude;
            if (distance >= travelDistance)
            {
                return lastPosition + (diff.normalized * travelDistance);
            }
            lastPosition = nextPosition;
            travelDistance -= distance;
        }

        return lastPosition;
    }

    public static Vector2 GetResultingPositionAlongPath(Vector2 lastPosition, Path movePath, float travelDistance)
    {
        for (int i = 0; i < movePath.Count; i++)
        {
            var nextPosition = movePath[i];
            var diff = (nextPosition - lastPosition);
            var distance = diff.magnitude;
            if (distance >= travelDistance)
            {
                return lastPosition + (diff.normalized * travelDistance);
            }
            travelDistance -= distance;
            lastPosition = nextPosition;
        }
        return lastPosition;
    }

    public static Path TransferMovePathToMotionPath(Vector2 lastPosition, Path movePath, float travelDistance)
    {
        var newPath = new Path();

        for (int i = 0; i < movePath.Count; i++)
        {
            var nextPosition = movePath[i];
            var diff = (nextPosition - lastPosition);
            var distance = diff.magnitude;
            if (distance >= travelDistance)
            {
                nextPosition = lastPosition + (diff.normalized * travelDistance);
                newPath.Add(nextPosition);
                break;
            }
            movePath.RemoveFirst();
            newPath.Add(nextPosition);
            travelDistance -= distance;
            lastPosition = nextPosition;
        }
        return newPath;
    }
}

static class IdGeneration
{
    public static string NewId()
    {
        string uuid = Guid.NewGuid().ToString();
        return uuid;
    }
}

public class HitCollider
{
    public int duration;
    public Vector2[] positions;
    public float[] radius;
    public float[] knockback;
    public int[] damage;
    public Vector2[] knockbackDirection;
    public string[] effects;
    public int stun;
    public HashSet<string> collidedEntities;

    public HitCollider(int duration, int collidersAmount, int effectsAmount, int stun)
    {
        this.duration = duration;
        this.positions = new Vector2[collidersAmount];
        this.radius = new float[collidersAmount];
        this.knockback = new float[collidersAmount];
        this.damage = new int[collidersAmount];
        this.knockbackDirection = new Vector2[collidersAmount];
        this.effects = new string[effectsAmount];
        this.stun = stun;
        this.collidedEntities = new HashSet<string>();
    }

    public HitCollider(Vector2 origin, Vector2 direction, CardCollider cardCollider)
    {
        this.positions = new Vector2[cardCollider.positions.Length];
        this.knockbackDirection = new Vector2[cardCollider.knockbackDirection.Length];

        var right = OperationUtils.PerpendicularVector(direction);
        for (int i = 0; i < cardCollider.positions.Length; i++)
        {
            var pos = cardCollider.positions[i];
            var dir = cardCollider.knockbackDirection[i];
            this.positions[i] = OperationUtils.OffsetPosition(origin,direction,pos);//origin + direction * pos.y + right * pos.x
            this.knockbackDirection[i] = OperationUtils.OffsetDirection(direction,dir);
        }

        this.duration = cardCollider.duration;
        this.radius = cardCollider.radius;
        this.knockback = cardCollider.knockback;
        this.damage = cardCollider.damage;
        this.effects = cardCollider.effects;
        this.stun = cardCollider.stun;
        this.collidedEntities = new HashSet<string>();
    }
    //if knockbackDirection.magnitude == 0,
    //knockback should be applied from center,
    // -> knockbackDirection = (targetPosition - HitCollider.position).Unit
}

class HitCollidersContainer
{
    public List<HitCollider> hitColliders;

    public HitCollidersContainer()
    {
        this.hitColliders = new List<HitCollider>();
    }
}

public class GameManagerScript : MonoBehaviour
{

    private Dictionary<string, Entity> entities = new Dictionary<string, Entity>();
    private Dictionary<string, GameObject> loadedPrefabs = new Dictionary<string, GameObject>();
    private Dictionary<string, Sprite> loadedSprites = new Dictionary<string, Sprite>();
    private Dictionary<string, EntityAnimation> loadedAnimations = new Dictionary<string, EntityAnimation>();
    private Dictionary<string, Effect> loadedEffects = new Dictionary<string, Effect>();
    private Dictionary<string, Card> loadedCards = new Dictionary<string, Card>();
    private Dictionary<string, AudioClip> loadedSounds = new Dictionary<string, AudioClip>();
    private GameEntity[] gameEntityResources;

    private Vector2 mapSize = new Vector2(10, 10);
    //private bool inBattle = false;
    private string mainEntityUuid;
    private string selectedEntityUuid;
    private List<string> controlEntities = new List<string>();
    private List<HitCollidersContainer> teamHitCollisions = new List<HitCollidersContainer>();
    private List<SpriteEffect> playingEffects = new List<SpriteEffect>();

    public GameObject cardPrefab;
    public GameObject spritePrefab;
    public Canvas screenCanvas;

    private float elapsedTime = 10;
    private float frameBlockAmount = 15f; 
    private float simulationFrames = 0;
    private bool gameEnded = false;

    private Path drawnPath = new Path();
    private float totalDrawnDistance = 0f;
    private float drawDistance = 1f;
    private float maxDrawAngle = 60f;

    private Vector3 lastMousePosition;
    private Vector3 cameraOffset;
    private float maxCameraOffset = 6f;
    private Vector3 originalCameraPosition;
    
    public Camera pixelCamera;
    public Image fadeScreen;
    public Camera mainCamera;
    public Image[] UICorners;

    private Vector3 cameraXVec = (Vector3.forward + Vector3.right).normalized;
    private Vector3 cameraYVec = (-Vector3.forward + Vector3.right).normalized;

    private DeckUIHandler deckHandler;
    private EnemyAIHandler enemyAIHandler;
    private bool ranEnemyCalculation = false;

    private List<GameObject> markers = new List<GameObject>();
    private List<GameObject> enemyMarkers = new List<GameObject>();

    private int enemyAmount = 1;

    void Start()
    {
        gameEntityResources = Resources.LoadAll<GameEntity>("Entities");

        enemyAIHandler = new EnemyAIHandler(entities);

        originalCameraPosition = mainCamera.transform.position;

        mainEntityUuid = CreateNewControllableEntity("condemned", 0f, 0f, 0);
        entities[mainEntityUuid].speed = 4f;
        entities[mainEntityUuid].friction = 2f;
        entities[mainEntityUuid].radius = 0.5f;
        entities[mainEntityUuid].modelHeight = 0.65f;
        entities[mainEntityUuid].health = 20;
        entities[mainEntityUuid].maxHealth = 20;
        var idle = LoadAnimation("condemned_idle");
        var walk = LoadAnimation("condemned_walk");
        var hit = LoadAnimation("condemned_hit");

        deckHandler = new DeckUIHandler(entities[mainEntityUuid].deck);

        AddItemToEntity(mainEntityUuid, "sword", "Weapon_R");

        entities[mainEntityUuid].animator.AddAnimation("idle", idle);
        entities[mainEntityUuid].animator.AddAnimation("walk", walk);
        entities[mainEntityUuid].animator.AddAnimation("hit", hit);
        entities[mainEntityUuid].animator.SetState(0);

        for (int i = 0;i < gameEntityResources.Length; i++)
        {
            if (gameEntityResources[i].entityId == "condemned")
            {
                foreach (var cardId in gameEntityResources[i].cardIds)
                {
                    entities[mainEntityUuid].deck.cards.Add(LoadCard(cardId));
                }
                break;
            }
        }

        teamHitCollisions.Add(new HitCollidersContainer());
        teamHitCollisions.Add(new HitCollidersContainer());

        SimulationStep();
    }

    Card LoadCard(string cardId)
    {
        Card loadedCard;
        if (loadedCards.ContainsKey(cardId))
        {
            loadedCard = loadedCards[cardId];
        }
        else
        {
            loadedCard = Resources.Load<Card>("Cards/" + cardId);
            loadedCards[cardId] = loadedCard;
        }

        return loadedCard;
    }

    CardContainer CreateNewCardContainer(Card loadedCard)
    {
        var cardGameObj = Instantiate(cardPrefab);
        var backImage = cardGameObj.transform.Find("BackImage").GetComponent<Image>();
        cardGameObj.transform.Find("FrontImage").Find("CardTitle").gameObject.GetComponent<TMP_Text>().text = loadedCard.title;
        cardGameObj.transform.Find("IconImage").GetComponent<Image>().sprite = loadedCard.image;
        cardGameObj.transform.SetParent(screenCanvas.transform, false);
        cardGameObj.transform.localScale = new Vector3(1, 1, 1);
        var cardCont = new CardContainer(cardGameObj, backImage, loadedCard);
        return cardCont;
    }

    CardContainer CreateNewCardContainer(string cardId)
    {
        var loadedCard = LoadCard(cardId);

        var cardGameObj = Instantiate(cardPrefab);
        var backImage = cardGameObj.transform.Find("BackImage").GetComponent<Image>();
        cardGameObj.transform.Find("FrontImage").Find("CardTitle").gameObject.GetComponent<TMP_Text>().text = loadedCard.title;
        cardGameObj.transform.Find("IconImage").GetComponent<Image>().sprite = loadedCard.image;
        cardGameObj.transform.SetParent(screenCanvas.transform,false);
        cardGameObj.transform.localScale = new Vector3(1, 1, 1);
        var cardCont = new CardContainer(cardGameObj, backImage, loadedCard);
        return cardCont;
    }

    public AudioClip LoadSound(string soundId)
    {
        AudioClip sound;
        if (loadedSounds.ContainsKey(soundId))
        {
            sound = loadedSounds[soundId];
        }
        else
        {
            sound = Resources.Load<AudioClip>("Sounds/" + soundId);
            loadedSounds[soundId] = sound;
        }
        return sound;
    }

    public void PlaySound(string soundId)
    {
        var soundClip = LoadSound(soundId);
        AudioSource.PlayClipAtPoint(soundClip, mainCamera.transform.position);
    }

    GameObject LoadEntityCircle(float radius, Color color)
    {
        var circle = LoadSprite("white_circle");
        circle.transform.rotation = Quaternion.Euler(-90f, 0, 0);
        circle.transform.localScale = new Vector3(radius*2,radius*2, 1f);
        circle.GetComponent<SpriteRenderer>().color = color;
        return circle;
    }

    GameObject LoadSprite(string spriteId, Color color)
    {
        Sprite sprite;
        if (loadedSprites.ContainsKey(spriteId))
        {
            sprite = loadedSprites[spriteId];
        }
        else
        {
            sprite = Resources.Load<Sprite>("Sprites/" + spriteId);
            loadedSprites[spriteId] = sprite;
        }
        var go = new GameObject("NewSprite");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = color;
        sr.sprite = sprite;
        return go;
    }

    GameObject LoadSprite(string spriteId)
    {
        Sprite sprite;
        if (loadedSprites.ContainsKey(spriteId))
        {
            sprite = loadedSprites[spriteId];
        }
        else
        {
            sprite = Resources.Load<Sprite>("Sprites/" + spriteId);
            loadedSprites[spriteId] = sprite;
        }
        var go = new GameObject("NewSprite");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        return go;
    }

    void AddItemToEntity(Entity entity, string itemId, string boneId)
    {
        if (entity.itemId == itemId) return;

        if (entity.itemId != null) Destroy(entity.itemObject);

        GameObject prefab;
        if (loadedPrefabs.ContainsKey(itemId))
        {
            prefab = loadedPrefabs[itemId];
        }
        else
        {
            prefab = Resources.Load<GameObject>("Items/" + itemId);
            loadedPrefabs[itemId] = prefab;
        }

        var obj = Instantiate(prefab);
        obj.transform.position = new Vector3(0, 0, 0);
        var bone = entity.animator.armature.GetComponentsInChildren<Transform>()
                      .FirstOrDefault(t => t.name == boneId);
        obj.transform.parent = bone;
        obj.transform.localRotation = Quaternion.Euler(0,45f,0);
        obj.transform.localPosition = Vector3.zero;
        entity.itemId = itemId;
        entity.itemObject = obj;
    }

    void AddItemToEntity(string entityUuid, string itemId, string boneId)
    {
        AddItemToEntity(entities[entityUuid], itemId, boneId);
    }

    EntityAnimation LoadAnimation(string animationId)
    {
        if (loadedAnimations.ContainsKey(animationId))
        {
            return loadedAnimations[animationId];
        }

        var animation = Resources.Load<AnimationObject>("Animations/"+animationId);
        if (animation != null)
        {
            return new EntityAnimation(animation);
        }

        return null;
    }

    string CreateNewControllableEntity(string entityId, float xPos, float yPos, int team)
    {
        GameObject prefab;
        if (loadedPrefabs.ContainsKey(entityId))
        {
            prefab = loadedPrefabs[entityId];
        } else
        {
            prefab = Resources.Load<GameObject>("EntityModels/" + entityId);
            loadedPrefabs[entityId] = prefab;
        }
        var obj = Instantiate(prefab);
        obj.transform.position = new Vector3(xPos,0,yPos);
        var uuid = IdGeneration.NewId();
        var circle = LoadEntityCircle(0.5f, new Color(0,0.5f,0.5f,1));
        var newEntity = new Entity(uuid, entityId, xPos, yPos, team, obj, circle);
        newEntity.circleColor = new Color(0, 1f, 1f, 1);
        entities[uuid] = newEntity;
        controlEntities.Add(uuid);
        return uuid;
    }

    string CreateNewEntity(string entityId, float xPos, float yPos, int team)
    {
        GameObject prefab;
        if (loadedPrefabs.ContainsKey(entityId))
        {
            prefab = loadedPrefabs[entityId];
        }
        else
        {
            prefab = Resources.Load<GameObject>("EntityModels/" + entityId);
            loadedPrefabs[entityId] = prefab;
        }
        var obj = Instantiate(prefab);
        obj.transform.position = new Vector3(xPos, 0, yPos);
        var uuid = IdGeneration.NewId();
        var circle = LoadEntityCircle(0.5f, new Color(0.5f,0,0.1f, 1));
        var newEntity = new Entity(uuid, entityId, xPos, yPos, team, obj, circle);
        newEntity.circleColor = new Color(1f, 0, 0.2f, 1);
        entities[uuid] = newEntity;
        return uuid;
    }

    void SpawnEffectOnEntity(Entity entity, string effectId, Vector3 position)
    {
        
    }

    void SpawnEffect(string effectId, Vector2 position, Vector2 direction, Color color)
    {
        Effect effect;
        if (loadedEffects.ContainsKey(effectId))
        {
            effect = loadedEffects[effectId];
        }
        else
        {
            effect = Resources.Load<Effect>("Effects/" + effectId);
            loadedEffects[effectId] = effect;
        }

        var obj = Instantiate(spritePrefab);
        
        if (effect.effectType == EffectType.Grounded)
        {
            obj.transform.position = new Vector3(position.x, 0.05f, position.y);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            obj.transform.rotation = Quaternion.Euler(-90f,0,angle);
        } else if (effect.effectType == EffectType.Directional)
        {
            obj.transform.position = new Vector3(position.x, 0.5f, position.y);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            obj.transform.rotation = Quaternion.Euler(-90f, 0, angle);
        } else if (effect.effectType == EffectType.FaceCamera)
        {
            obj.transform.position = new Vector3(position.x, effect.size / 3f, position.y);
            OperationUtils.SpriteFaceCameraAndAim(obj.transform, direction, mainCamera);
        }
        
        var spriteEffect = new SpriteEffect(effect, obj);
        obj.transform.localScale = new Vector3(effect.size, effect.size, effect.size);
        spriteEffect.spriteRenderer.sprite = effect.sprites[0];
        spriteEffect.spriteRenderer.color = color;
        playingEffects.Add(spriteEffect);
    }

    void SpawnEffect(string effectId, Vector2 position, Vector2 direction)
    {
        Effect effect;
        if (loadedEffects.ContainsKey(effectId))
        {
            effect = loadedEffects[effectId];
        }
        else
        {
            effect = Resources.Load<Effect>("Effects/" + effectId);
            loadedEffects[effectId] = effect;
        }

        var obj = Instantiate(spritePrefab);
        if (effect.effectType == EffectType.Grounded)
        {
            obj.transform.position = new Vector3(position.x, 0.05f, position.y);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            obj.transform.rotation = Quaternion.Euler(-90f, 0, angle);
        }
        else if (effect.effectType == EffectType.Directional)
        {
            obj.transform.position = new Vector3(position.x, 0.5f, position.y);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            obj.transform.rotation = Quaternion.Euler(-90f, 0, angle);
        }
        else if (effect.effectType == EffectType.FaceCamera)
        {
            obj.transform.position = new Vector3(position.x, effect.size / 3f, position.y);
            OperationUtils.SpriteFaceCameraAndAim(obj.transform, direction, mainCamera);
        }

        var spriteEffect = new SpriteEffect(effect, obj);
        obj.transform.localScale = new Vector3(effect.size, effect.size, effect.size);
        spriteEffect.spriteRenderer.sprite = effect.sprites[0];
        playingEffects.Add(spriteEffect);
    }

    void SpawnPlayer()
    {

    }

    void SpawnEnemy()
    {
        var random = UnityEngine.Random.Range(0, gameEntityResources.Length);

        var enemyResource = gameEntityResources[random];

        if (enemyResource.entityId != "condemned")
        {
            var x = UnityEngine.Random.Range(-8, 9);
            var y = UnityEngine.Random.Range(-8, 9);
            var enemyUuid = CreateNewEntity(enemyResource.modelId, x,y,1);
            entities[enemyUuid].speed = enemyResource.speed;
            entities[enemyUuid].friction = enemyResource.friction;
            entities[enemyUuid].radius = enemyResource.radius;
            entities[enemyUuid].modelHeight = enemyResource.modelHeight;
            entities[enemyUuid].health = enemyResource.maxHealth;
            entities[enemyUuid].maxHealth = enemyResource.maxHealth;
            var idle = LoadAnimation(enemyResource.animations[0]);
            var walk = LoadAnimation(enemyResource.animations[1]);
            var hit = LoadAnimation(enemyResource.animations[2]);
            entities[enemyUuid].animator.AddAnimation("idle", idle);
            entities[enemyUuid].animator.AddAnimation("walk", walk);
            entities[enemyUuid].animator.AddAnimation("hit", hit);
            entities[enemyUuid].animator.SetState(0);
            entities[enemyUuid].targetUuid = mainEntityUuid;

            foreach (var cardId in enemyResource.cardIds)
            {
                entities[enemyUuid].deck.cards.Add(LoadCard(cardId));
            }
        }

        
    }

    void PresetLevel()
    {

    }

    void NewLevel()
    {

    }

    void AddEnemyMarker(Vector2 position)
    {
        var marker = LoadSprite("white_box", new Color(1,0,0.1f,1));
        marker.transform.position = new Vector3(position.x, 0.05f, position.y);
        marker.transform.rotation = Quaternion.Euler(-90f, 0, 0);
        marker.transform.localScale = new Vector3(0.15f, 0.15f, 1f);
        enemyMarkers.Add(marker);
    }

    void AddPlayerMarker(Vector2 position)
    {
        var marker = LoadSprite("white_box", new Color(0, 1, 1, 1));
        marker.transform.position = new Vector3(position.x, 0.05f, position.y);
        marker.transform.rotation = Quaternion.Euler(-90f, 0, 0);
        marker.transform.localScale = new Vector3(0.15f, 0.15f, 1f);
        markers.Add(marker);
    }

    void ClearDrawnPath()
    {
        totalDrawnDistance = 0;
        drawnPath.Clear();
        ClearMarkers();
        RemoveEnemyMarkers();
    }

    void ClearMarkers()
    {
        foreach (var marker in markers)
        {
            Destroy(marker);
        }
        markers.Clear();
    }

    void AddCardCollidersToHitColliders(Vector2 origin, Vector2 direction, CardCollider[] cardColliders, List<HitCollider> colliders)
    {
        for (int i = cardColliders.Length-1; i >= 0; i--)
        {
            colliders.Add(new HitCollider(origin, direction, cardColliders[i]));
        }
    }

    void RunAction(Entity entity, CardAction action) {

        var targetUuid = entity.targetUuid;
        var targetPosition = entity.targetPosition;
        var targetDirection = entity.targetDirection;

        if (action.faceTarget)
        {
            if (targetUuid != null && entities.ContainsKey(targetUuid))
            {
                var diff = (entities[targetUuid].position - entity.position).normalized;
                var angle = OperationUtils.DirectionToAngle(diff);
                entity.rotation = angle;
                entity.targetRotation = angle;
                entity.targetDirection = diff;
            } else if (targetDirection.sqrMagnitude > 0)
            {
                var angle = OperationUtils.DirectionToAngle(targetDirection);
                entity.rotation = angle;
                entity.targetRotation = angle;
            } else
            {
                var diff = (targetPosition - entity.position).normalized;
                var angle = OperationUtils.DirectionToAngle(diff);
                entity.rotation = angle;
                entity.targetRotation = angle;;
                entity.targetDirection = diff;
            }
        }
        
        if (entity.nameId == "condemned" && !string.IsNullOrWhiteSpace(action.swapItemId))
        {
            AddItemToEntity(entity,action.swapItemId,"Weapon_R");
        }

        if (!string.IsNullOrWhiteSpace(action.animationId))
        {
            var animation = LoadAnimation(action.animationId);
            entity.animator.AddAnimation(action.animationId, animation);
            entity.animator.PlayAnimation(action.animationId);
        }

        if (action.cardColliders.Length > 0)
        {
            AddCardCollidersToHitColliders(entity.position, entity.targetDirection, action.cardColliders, teamHitCollisions[entity.team].hitColliders);
        }

        if (action.visualEffects.Length > 0)
        {
            var forward = entity.targetDirection.normalized;
            var right = OperationUtils.PerpendicularVector(forward);

            foreach (var visualEffect in action.visualEffects)
            {
                /*
                public string effectId;
                public Vector2 worldOffset;
                public Vector2 localOffset;
                public bool onSelf;
                public Quaternion rotation;
                public string boneId;
                */
                var pos = OperationUtils.OffsetPosition(entity.position,forward,visualEffect.localOffset);
                SpawnEffect(visualEffect.effectId, pos, forward);
            }   
        }

        if (action.applyVelocity.sqrMagnitude > 0)
        {
            Debug.Log("APPLIED");
            entity.moveVelocity += entity.targetDirection * action.applyVelocity.y + new Vector2(-entity.targetDirection.y,entity.targetDirection.x) * action.applyVelocity.x;
        }

        if (!string.IsNullOrWhiteSpace(action.soundId))
        {
            PlaySound(action.soundId);
        }

    }

    void UpdateActions(Entity entity)
    {

        /*
            public int actionDelay;
            public string swapItemId;
            public string animationId;
            public string substitutionId;
            public CardCollider[] cardColliders;
            public VisualEffect[] visualEffects;
            public Vector2 applyVelocity;
            public string soundId;
         */

        for (int i = entity.actionQueue.Count - 1; i >= 0; i--)
        {
            var action = entity.actionQueue[i];
            action.delay--;
            Debug.Log(action.delay);
            if (action.delay <= 0) {
                RunAction(entity, action);
                entity.actionQueue.RemoveAt(i);
            }
        }
    }

    void DamageEntity(Entity entity, List<PathCollisionResult> collisionResults)
    {
        if (collisionResults.Count > 0)
        {
            foreach (var collisionResult in collisionResults)
            {
                entity.HitWithCollision(collisionResult);
                var direction = collisionResult.knockbackDirection;
                SpawnEffect("sparks", entity.position + direction * entity.radius*2.5f, direction, new Color(0.6f,0,0.1f,1));
            }
            int zeroOrOne = UnityEngine.Random.Range(1, 3);
            PlaySound("hit" + zeroOrOne);
        }

    }

    void SpawnRandomEnemy()
    {

    }

    void SimulationStep()
    {

        float delta = 1f / 30f;

        // # Initial positions pass
        foreach (var pair in entities)
        {
            var uuid = pair.Key;
            var entity = pair.Value;

            if (entity.stun > 0 || entity.actionDelay > 0)
            {
                entity.moveMode = MoveMode.MoveVelocity;
            } else
            {
                entity.moveVelocity = new Vector2(0, 0);
                entity.moveMode = MoveMode.MovePath;
                if (entity.movePath.Count > 0)
                {
                    entity.animator.SetState(1);
                }
                else
                {
                    entity.animator.SetState(0);
                }
            }
            
            if (entity.movePath.Count == 0)
            {
                entity.UpdateCardQueue();
            }

            UpdateActions(entity);
        }

        // # Collision and logic pass
        foreach (var pair in entities)
        {
            var uuid = pair.Key;
            var entity = pair.Value;

            var team = entity.team;
            var container = teamHitCollisions[Mathf.Abs(team - 1)]; //TODO: account for all teams
            var colliders = container.hitColliders;

            // Loop store variables
            var travelDistance = entity.speed * delta;
            var resultingPosition = entity.position;
            var pathHitColliders = new List<HitCollider>();
            var pathCollisions = new List<PathCollisionResult>();
            var closestCollisionDistance = travelDistance * 2;

            var collisionsDamageList = new List<PathCollisionResult>();

            if (entity.moveMode == MoveMode.MoveVelocity) {
                var velocity = entity.moveVelocity.magnitude;

                if (velocity <= -1e6f)
                {
                    for (int i = 0; i < colliders.Count; i++)
                    {
                        var hitCollider = colliders[i];
                        if (!hitCollider.collidedEntities.Contains(uuid))
                        {
                            var collisionResult = OperationUtils.EntityRadiusCollision(entity.position, entity.radius, hitCollider);
                            if (collisionResult.hasCollided)
                            {
                                collisionsDamageList.Add(collisionResult);
                                hitCollider.collidedEntities.Add(uuid);
                            }
                        }
                    }
                } else
                {
                    var nextEntityPosition = entity.position + entity.moveVelocity * delta;
                    var frictionResult = Mathf.Clamp(entity.friction * delta, 0f, 1e6f);
                    entity.moveVelocity = entity.moveVelocity - entity.moveVelocity * frictionResult;

                    for (int i = 0; i < colliders.Count; i++)
                    {
                        var hitCollider = colliders[i];
                        if (!hitCollider.collidedEntities.Contains(uuid))
                        {
                            var collisionResult = OperationUtils.EntitySegmentCollision(entity.position, nextEntityPosition, entity.radius, hitCollider);
                            if (collisionResult.hasCollided)
                            {
                                hitCollider.collidedEntities.Add(uuid);
                                var traveledDistance = collisionResult.traveledDistance;
                                if (traveledDistance < closestCollisionDistance)
                                {
                                    closestCollisionDistance = traveledDistance;
                                    pathCollisions.Insert(0, collisionResult);
                                    pathHitColliders.Insert(0, hitCollider);
                                    continue;
                                }
                                pathCollisions.Add(collisionResult);
                                pathHitColliders.Add(hitCollider);
                            }
                        }
                    }

                    resultingPosition = nextEntityPosition;
                }
            }
            else {
                var motionPath = OperationUtils.TransferMovePathToMotionPath(entity.position, entity.movePath, travelDistance);

                for (int i = 0; i < colliders.Count; i++)
                {
                    var hitCollider = colliders[i];
                    if (!hitCollider.collidedEntities.Contains(uuid))
                    {
                        var collisionResult = (motionPath.Count > 0)
                            ? OperationUtils.EntityPathCollision(entity, motionPath, hitCollider)
                            : OperationUtils.EntityRadiusCollision(entity.position,entity.radius,hitCollider);

                        if (collisionResult.hasCollided)
                        {
                            hitCollider.collidedEntities.Add(uuid);
                            var traveledDistance = collisionResult.traveledDistance;
                            if (traveledDistance < closestCollisionDistance)
                            {
                                closestCollisionDistance = traveledDistance;
                                pathCollisions.Insert(0, collisionResult); //insert closest to first
                                pathHitColliders.Insert(0, hitCollider);
                                continue;
                            }
                            pathCollisions.Add(collisionResult);
                            pathHitColliders.Add(hitCollider);
                        }
                    }
                }

                if (motionPath.Count > 0) resultingPosition = motionPath[motionPath.Count-1];
            }

            if (pathHitColliders.Count > 0)
            {
                var entityPositionAfterCollision = pathCollisions[0].entityPosition; // Position where the entity will be placed after the hit
                for (int i = pathCollisions.Count - 1; i >= 1; i--)
                {
                    var stillCollides = OperationUtils.DoesEntityCollide(entityPositionAfterCollision, entity.radius, colliders[i]);

                    if (!stillCollides)
                    {
                        pathCollisions.RemoveAt(i);
                        pathHitColliders.RemoveAt(i);
                        continue;
                    }
                    collisionsDamageList.Add(pathCollisions[i]);
                    //entity.HitWithCollision(pathCollisions[i]);
                }
                collisionsDamageList.Add(pathCollisions[0]);
                //entity.HitWithCollision(pathCollisions[0]);
                entity.MoveTo(entityPositionAfterCollision);//, pathCollisions[0].entityRotation);//OperationUtils.DirectionToAngle(entityPositionAfterCollision - entity.position));
            }
            else
            {
                if (entity.stun > 0)
                {
                    entity.MoveTo(resultingPosition);
                } else
                {
                    if (entity.movePath.Count > 0) entity.MoveTo(resultingPosition, OperationUtils.DirectionToAngle(resultingPosition - entity.position));
                    else entity.MoveTo(resultingPosition);
                }
            }

            DamageEntity(entity, collisionsDamageList);

            if (entity.actionDelay > 0) entity.actionDelay--;
        }

        // # Animation pass
        foreach (var pair in entities)
        {
            var uuid = pair.Key;
            var entity = pair.Value;

            entity.animator.Step();
            entity.UpdateRotation(delta);
            if (entity.stun > 0) entity.stun--;
        }

        // # Effects pass
        for (var i = playingEffects.Count-1; i >= 0 ; i--)
        {
            var effect = playingEffects[i];
            effect.Step();
            if (effect.totalPlayedFrames > effect.duration) {
                Destroy(effect.obj);
                playingEffects.RemoveAt(i);
            }
        }

        // # Colliders removal pass
        for (var i = teamHitCollisions.Count - 1; i >= 0; i--)
        {
            var collisionContainer = teamHitCollisions[i];
            for (var j = collisionContainer.hitColliders.Count-1; j >= 0 ; j--)
            {
                var hitCol = collisionContainer.hitColliders[j];
                hitCol.duration--;
                if (hitCol.duration <= 0)
                {
                    collisionContainer.hitColliders.RemoveAt(j);
                }
            }
        }

        List<string> uuidsToRemove = new List<string>();
        // # Death pass
        foreach (var pair in entities)
        {
            var uuid = pair.Key;
            var entity = pair.Value;

            if (entity.health <= 0)
            {
                if (uuid == mainEntityUuid)
                {
                    deckHandler.DiscardAll();
                    gameEnded = true;
                    simulationFrames = 0;
                    return;
                }

                //SpawnEffect("death",entity.position);
                //entity.Kill();
                Destroy(entity.entityCircle);
                Destroy(entity.gameObject);
                uuidsToRemove.Add(uuid);
            }
        }

        foreach (var key in uuidsToRemove)
        {
            entities.Remove(key);
        }
    }

    void DrawPath(Vector2 mousePos)
    {
        if (drawnPath.Count < 1)
        {
            var entityPos = entities[mainEntityUuid].position;
            var diff = mousePos - entityPos;
            var distance = diff.magnitude;
            if (distance >= drawDistance)
            {
                var newPos = entityPos + diff.normalized * drawDistance;
                totalDrawnDistance += distance;
                drawnPath.Add(newPos);
                AddPlayerMarker(newPos);
            }
        }
        else
        {
            var beforeLastPoint = (drawnPath.Count == 1) ? entities[mainEntityUuid].position : drawnPath[drawnPath.Count - 2];
            var lastPoint = drawnPath[drawnPath.Count - 1];
            var lastDiff = lastPoint - beforeLastPoint;
            var diff = mousePos - lastPoint;
            var distance = diff.magnitude;
            if (distance >= drawDistance)
            {
                var angle = Vector2.SignedAngle(lastDiff, diff);

                if (Mathf.Abs(angle) < maxDrawAngle)
                {
                    var newPos = lastPoint + diff.normalized * drawDistance;
                    AddPlayerMarker(newPos);
                    totalDrawnDistance += distance;
                    drawnPath.Add(newPos);
                }
                else
                {
                    var rotatedVec = OperationUtils.RotateVector(lastDiff.normalized, Mathf.Sign(angle) * maxDrawAngle);
                    var newPoint = lastPoint + rotatedVec * drawDistance;

                    if ((mousePos - newPoint).sqrMagnitude < drawDistance * drawDistance * 2)
                    {
                        totalDrawnDistance += distance;
                        AddPlayerMarker(newPoint);
                        drawnPath.Add(newPoint);
                    }
                }
            }
        }
    }

    void RemoveEnemyMarkers()
    {
        foreach (var marker in enemyMarkers)
        {
            Destroy(marker);
        }
    }

    void DrawEnemyMarkers(Path drawPath)
    {
        for (var i = 0; i < drawPath.Count; i++)
        {
            AddEnemyMarker(drawPath[i]);
        }
    }

    void EntitySelection(Vector2 mousePos)
    {
        var closestEntityUuid = "";
        var closestDistance = 1e6f;

        foreach (var pair in entities)
        {
            var uuid = pair.Key;
            var entity = pair.Value;

            var distance = (entity.position - mousePos).magnitude;

            if (distance < closestDistance && distance < entity.radius)
            {
                closestDistance = distance;
                closestEntityUuid = uuid;
            }
        }

        if (closestEntityUuid != "")
        {
            var currentEntity = entities[closestEntityUuid];

            if (selectedEntityUuid == null)
            {
                selectedEntityUuid = closestEntityUuid;
                currentEntity.entityCircle.GetComponent<SpriteRenderer>().color = currentEntity.circleColor;

                if (currentEntity.team != 0)
                {
                    DrawEnemyMarkers(currentEntity.movePath);
                }
            }
            else if (selectedEntityUuid != closestEntityUuid)
            {
                var lastEntity = entities[selectedEntityUuid];
                lastEntity.entityCircle.GetComponent<SpriteRenderer>().color = OperationUtils.DimColor(lastEntity.circleColor);
                selectedEntityUuid = closestEntityUuid;
                currentEntity.entityCircle.GetComponent<SpriteRenderer>().color = currentEntity.circleColor;

                RemoveEnemyMarkers();

                if (currentEntity.team != 0)
                {
                    DrawEnemyMarkers(currentEntity.movePath);
                }
            }
        }
        else
        {
            if (selectedEntityUuid != null)
            {
                var lastEntity = entities[selectedEntityUuid];
                lastEntity.entityCircle.GetComponent<SpriteRenderer>().color = OperationUtils.DimColor(lastEntity.circleColor);
                selectedEntityUuid = null;

                RemoveEnemyMarkers();
            }

        }
    }

    void Update()
    {

        var delta = Time.deltaTime;
        elapsedTime += delta;
        deckHandler.UpdateDeck(delta);

        var mouseScreenPos = Input.mousePosition;
        var mouseScreenPosCenterX = mouseScreenPos.x - 990;
        var mouseScreenPosY = mouseScreenPos.y;

        if (fadeScreen.color.a > 0 && !gameEnded)
        {
            fadeScreen.color = fadeScreen.color - new Color(0,0,0,delta/3f);
        } else if (gameEnded) {
            fadeScreen.color = fadeScreen.color + new Color(0, 0, 0, delta / 3f);

            if (fadeScreen.color.a >= 1)
            {
                var clamp = Mathf.Clamp01(fadeScreen.color.r + delta / 3f);
                fadeScreen.color = new Color(clamp, clamp, clamp, 1);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                SceneManager.LoadScene("Game");
            }
        }

        if (mouseScreenPosY < 200)
        {
            deckHandler.HoverCard(mouseScreenPosCenterX);
        }
        else
        {
            deckHandler.hoverId = -1;
        }

        if (simulationFrames > 0 && elapsedTime > 1 / 30f)
        {
            elapsedTime = 0;
            simulationFrames--;
            SimulationStep();
        }

        if (simulationFrames > 0)
        {
            if (UICorners[0].color.a < 1)
            {
                foreach (var image in UICorners)
                {
                    image.color = new Color(1, 1, 1, Mathf.Clamp01(image.color.a + delta * 3f));
                }
            }
        } else
        {
            if(!ranEnemyCalculation)
            {
                foreach(var pair in entities)
                {
                    var uuid = pair.Key;
                    var entity = pair.Value;

                    if (entity.team != 0)
                    {
                        enemyAIHandler.CalculateEntityTurn(entity);
                    }
                }

                ranEnemyCalculation = true;
            }

            if (UICorners[0].color.a  > 0)
            {
                foreach (var image in UICorners)
                {
                    image.color = new Color(1, 1, 1, Mathf.Clamp01(image.color.a- delta * 2f));
                }
            }

            if (Input.GetKeyDown(KeyCode.Space) && !gameEnded)
            {
                selectedEntityUuid = null;
                enemyAIHandler.ClearEndPositions();
                entities[mainEntityUuid].ClearStun();
                entities[mainEntityUuid].movePath = new Path(drawnPath);
                if (totalDrawnDistance > entities[mainEntityUuid].speed * 2)
                {
                    entities[mainEntityUuid].actionDelay = (int)(totalDrawnDistance / entities[mainEntityUuid].speed);
                }
                ClearDrawnPath();
                simulationFrames = 60;
                ranEnemyCalculation = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            var cards = entities[mainEntityUuid].deck.cards;
            Debug.Log(cards.Count);
            var rand = UnityEngine.Random.Range(0, cards.Count);
            var testCardCont = CreateNewCardContainer(cards[rand]);
            deckHandler.AddCardContainer(testCardCont);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            SpawnEnemy();
            SimulationStep();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            SimulationStep();
        }

        if (simulationFrames <= 0)
        {

            var mousePos = OperationUtils.GetMouseWorldPositionOrtho(mainCamera, 0);

            if (Input.GetMouseButtonDown(1))
            {
                ClearDrawnPath();
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (mouseScreenPos.y < 200)
                {
                    deckHandler.SelectCard(mouseScreenPosCenterX);
                } else if (totalDrawnDistance < entities[mainEntityUuid].speed * 2)
                {
                    if (deckHandler.selectedId > -1)
                    {
                        var entity = entities[mainEntityUuid];
                        var selId = deckHandler.selectedId;
                        if (selectedEntityUuid != null && entities[selectedEntityUuid].team != 0)
                        {
                            entity.QueueCard(deckHandler.cardContainers[selId].card, selectedEntityUuid);
                        } else
                        {
                            entity.QueueCard(deckHandler.cardContainers[selId].card, mousePos);
                        }
                        deckHandler.DiscardCard(selId);
                    }
                }
            }

            if (Input.GetMouseButton(1))
            {
                if (totalDrawnDistance < entities[mainEntityUuid].speed * 2)
                {
                    DrawPath(mousePos);
                }
            }

            EntitySelection(mousePos);

        }

        if (Input.GetMouseButtonDown(2))
        {
            lastMousePosition = mouseScreenPos;
        }

        if (Input.GetMouseButton(2))
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            cameraOffset += cameraXVec * mouseDelta.y * delta * 5f;
            cameraOffset += cameraYVec * mouseDelta.x * delta * 5f;
            cameraOffset = new Vector3(Mathf.Clamp(cameraOffset.x, -maxCameraOffset, maxCameraOffset), 0, Mathf.Clamp(cameraOffset.z, -maxCameraOffset, maxCameraOffset));
            var cameraPos = originalCameraPosition + cameraOffset;
            pixelCamera.transform.position = cameraPos;
            mainCamera.transform.position = cameraPos;

            lastMousePosition = Input.mousePosition;
        }
    }
}