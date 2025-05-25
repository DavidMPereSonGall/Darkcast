using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameAction
{
    public string actionType; // card, dash, attack
    //public Card card;
    public Vector2 position;
    public float delay;
}

class Entity
{
    public GameObject gameObject;
    public float x;
    public float y;
    public int team;

    public int health;
    public int maxHealth;
    public float speed;
    public float radius;
    public float stun;

    public List<Vector2> movePath;

    public List<GameAction> actionQueue;

    public Entity(string entityId, float xPos, float yPos, int team, GameObject obj)
    {
        this.gameObject = obj;
        this.x = xPos;
        this.y = yPos;
        this.team = team;
    }
}

public class GameManagerScript : MonoBehaviour
{

    private Vector2 mapSize = new Vector2(10, 10);
    private List<Entity> entityList = new List<Entity>();
    private bool inBattle = false;
    private int mainEntity = 0;
    private List<int> controlEntities = new List<int>();

    void Start()
    {
        CreateNewControllableEntity("condemned", 0f, 0f, 0);
        Debug.Log("created");

        CreateNewEntity("condemned", 2f, 2f, 1);
    }

    void CreateNewControllableEntity(string entityId, float xPos, float yPos, int team)
    {
        var prefab = Resources.Load<GameObject>("EntityModels/" + entityId);
        var obj = Instantiate(prefab);
        obj.transform.position = new Vector3(xPos,0,yPos);
        var newEntity = new Entity(entityId, xPos, yPos, team, obj);
        entityList.Add(newEntity);
        controlEntities.Add(entityList.Count);
    }

    void CreateNewEntity(string entityId, float xPos, float yPos, int team)
    {
        var prefab = Resources.Load<GameObject>("EntityModels/" + entityId);
        var obj = Instantiate(prefab);
        var newEntity = new Entity(entityId, xPos, yPos, team, obj);
        entityList.Add(newEntity);
    }

    void PresetLevel()
    {

    }

    void NewLevel()
    {

    }

    void Update()
    {
        
    }
}
