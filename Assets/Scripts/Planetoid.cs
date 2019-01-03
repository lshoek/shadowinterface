using UnityEngine;

public class Planetoid : MonoBehaviour
{
    public float Mass
    {
        get { return Collider.attachedRigidbody.mass; }
    }

    public float Size
    {
        get { return transform.localScale.x; }
    }

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
