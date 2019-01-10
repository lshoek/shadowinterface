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

    [SerializeField] private float defaultMaxSpeed = 2.0f;
    private float maxSpeed;

    private float defaultDrag;
    private float defaultSize;

    private bool explosionAnim = true;

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
        // type specific
        Color planetoidColor = (Type == PlanetoidType.HOSTILE) ? Application.Instance.Palette.LIGHTSALMONPINK : Color.white;
        Material.SetColor("_Color", planetoidColor);

        transform.LookAt(Application.Instance.GameManager.GravityBody.Position);
        Collider.attachedRigidbody.velocity = Vector3.ClampMagnitude(Collider.attachedRigidbody.velocity, maxSpeed);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (manager.State == GameManager.GameState.RUNNING)
        {
            if (Type == PlanetoidType.HOSTILE)
            {
                if (collision.gameObject.name == "ColliderMesh")
                {
                    manager.AddCollision(this);
                }
                if (collision.gameObject.name == "Planet")
                {
                    manager.SpawnCross(transform.position);
                    manager.StopGame();
                }
            }
            else
            {
                if (collision.gameObject.name == "ColliderMesh")
                {
                    manager.SpawnCross(transform.position);
                    manager.StopGame();
                }
                if (collision.gameObject.name == "Planet")
                {
                    manager.GravityBody.PlanetAnimator.Play("Inhabited");
                    manager.Dinosaurs++;
                    manager.AddCollision(this);
                    explosionAnim = false;
                }
            }
        }
    }

    void OnDestroy()
    {
        if (explosionAnim)
        {
            GameObject ob = Instantiate(Resources.Load("Prefabs/Explosion") as GameObject);
            ob.transform.position = transform.position;
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
