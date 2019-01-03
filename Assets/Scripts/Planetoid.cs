using UnityEngine;

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

    [SerializeField] private float hostileHue;
    [SerializeField] private float friendlyHue;

    [SerializeField] private float defaultMaxSpeed = 2.0f;
    private float maxSpeed;

    private float defaultDrag;
    private float defaultSize;

    void Awake()
    {
        Collider = GetComponent<Collider>();
        
        if (Type == PlanetoidType.HOSTILE)
            Material = GetComponent<Renderer>().material;

        defaultDrag = Collider.attachedRigidbody.drag;
        defaultSize = Collider.transform.localScale.x;

        maxSpeed = defaultMaxSpeed;
    }

    void Update()
    {
        // color flashing
        if (Type == PlanetoidType.HOSTILE)
            Material.SetColor("_Color", Color.HSVToRGB(hostileHue, Application.Instance.GameManager.Turb, 1.0f));
        else
            Material.SetColor("_Color", Color.HSVToRGB(friendlyHue, Application.Instance.GameManager.Turb, 1.0f));

        Collider.attachedRigidbody.velocity = Vector3.ClampMagnitude(Collider.attachedRigidbody.velocity, maxSpeed);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "ColliderMesh")
        {
            manager.AddCollision(this);
            // play explosion animation
        }
        if (collision.gameObject.name == "Planet")
        {
            manager.StopGame();
            // play hit animation for planet and explosion for planetoid
            // player takes damage or loses game
        }
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
