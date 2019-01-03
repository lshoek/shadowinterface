using UnityEngine;

public class Planetoid : MonoBehaviour
{
    public GameManager Manager { set { manager = value; } }
    public float Mass
    {
        get { return Collider.attachedRigidbody.mass; }
    }

    public float Size
    {
        get { return transform.localScale.x; }
    }
    private GameManager manager;

    public Collider Collider;
    public Material Material;

    private float defaultMass;
    private float defaultSize;

    [SerializeField] private float defaultMaxSpeed = 2.0f;
    private float maxSpeed;

    void Awake()
    {
        Collider = GetComponent<Collider>();
        Material = GetComponent<Renderer>().material;

        defaultMass = Collider.attachedRigidbody.mass;
        defaultSize = Collider.transform.localScale.x;

        maxSpeed = defaultMaxSpeed;
    }

    void FixedUpdate()
    {
        Collider.attachedRigidbody.velocity = Vector3.ClampMagnitude(Collider.attachedRigidbody.velocity, maxSpeed);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "TestMesh")
        {
<<<<<<< HEAD
            Debug.Log("Mesh Hit!!!");
            Application.Instance.GameManager.DespawnPlanetoid(GetComponent<Planetoid>());
=======
            //manager.
            // play explosion animation
            manager.DespawnPlanetoid(this);
>>>>>>> 1cac12f3f3465559857c2561277e871275675b06
        }


        if (collision.gameObject.name == "Planet")
        {
<<<<<<< HEAD
            Debug.Log("Planet Hit!!!");
            Application.Instance.GameManager.StopGame();
=======
            
            // play hit animation for planet and explosion for planetoid
            // player takes damage or loses game
>>>>>>> 1cac12f3f3465559857c2561277e871275675b06
        }

    }

    public void MultiplyMass(float scalar)
    {
        Collider.attachedRigidbody.mass = defaultMass * scalar;
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
