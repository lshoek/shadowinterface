﻿using UnityEngine;

public class Planetoid : MonoBehaviour
{
    public enum PlanetoidType
    {
        HOSTILE,
        FRIENDLY
    }
    
    [SerializeField] private PlanetoidType type;
    public PlanetoidType Type { get { return type; } }

    private GameManager manager;
    public GameManager Manager { set { manager = value; } }

    public float Drag { get { return Collider.attachedRigidbody.drag; } }
    public float Size { get { return transform.localScale.x; } }

    [HideInInspector] public Collider Collider;
    [HideInInspector] public Material Material = null;

    [SerializeField] private float defaultMaxSpeed = 2.0f;
    private float maxSpeed;

    private float defaultDrag;
    private float defaultSize;

    void Awake()
    {
        Collider = GetComponent<Collider>();
        Material = GetComponent<Renderer>().material;

        defaultDrag = Collider.attachedRigidbody.drag;
        defaultSize = Collider.transform.localScale.x;

        maxSpeed = defaultMaxSpeed;
    }

    void Update()
    {
        // color flashing
        if (Type == PlanetoidType.HOSTILE)
            Material.SetColor("_Color", Color.HSVToRGB(Application.Instance.GameManager.FriendlyHue, Application.Instance.GameManager.Turb, 1.0f));
        else
            Material.SetColor("_Color", Color.HSVToRGB(Application.Instance.GameManager.HostileHue, Application.Instance.GameManager.Turb, 1.0f));

        Collider.attachedRigidbody.velocity = Vector3.ClampMagnitude(Collider.attachedRigidbody.velocity, maxSpeed);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "ColliderMesh")
        {
            manager.AddCollision(this);
        }
        if (collision.gameObject.name == "Planet")
        {
            GameObject ob = Instantiate(Resources.Load("Prefabs/CrossMark") as GameObject);
            ob.transform.position = transform.position;

            manager.StopGame();
        }
    }

    void OnDestroy()
    {
        GameObject ob = Instantiate(Resources.Load("Prefabs/Explosion") as GameObject);
        ob.transform.position = transform.position;
    }

    public void MultiplyDrag(float scalar)
    {
        Collider.attachedRigidbody.drag = defaultDrag * scalar;
    }

    public void MultiplySize(float scalar)
    {
        float scale = defaultSize * scalar;
        Collider.transform.localScale = new Vector3(scale, scale, scale);
    }

    public void MultiplyMaxSpeed(float scalar)
    {
        maxSpeed = defaultMaxSpeed * scalar;
    }
}
