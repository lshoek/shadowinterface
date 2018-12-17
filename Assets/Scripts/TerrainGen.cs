using UnityEngine;

public class TerrainGen : MonoBehaviour
{
    public GameObject ColliderParent;

    const int MAX_COLLIDERS = 45;

    [SerializeField] float spawnTimeStep = 0.1f;
    [SerializeField] float capsuleWidth = 0.25f;

    float spawnAngle = 0.0f;
    float timeLastSpawned = 0.0f;

    CapsuleCollider[] colliders;
    Vector3[] positions = new Vector3[MAX_COLLIDERS];

    int positionCounter = 0;

    public PhysicMaterial physicMaterial;

    void Start()
    {
        colliders = new CapsuleCollider[MAX_COLLIDERS];

        for (int i = 0; i < MAX_COLLIDERS; i++)
        {
            CapsuleCollider collider = GameObject.CreatePrimitive(PrimitiveType.Cylinder).GetComponent<CapsuleCollider>();

            collider.transform.SetParent(ColliderParent.transform);
            collider.transform.localScale *= capsuleWidth;
            collider.material = Resources.Load("Resources/Materials/Physic/LineColliderPhysicMaterial") as PhysicMaterial;
            collider.center = Vector3.zero;
            colliders[i] = collider;
        }
        IntializeColliders();
    }

    void Update()
    {
        float elapsedTime = Time.time;

        if (elapsedTime - timeLastSpawned > spawnTimeStep)
        {
            positionCounter++;
            positionCounter %= MAX_COLLIDERS;
            positions[positionCounter] = GetNewLocation(positionCounter);
            
            AddColliderToLine(positionCounter);
            timeLastSpawned = elapsedTime;
        }
    }

    private void IntializeColliders()
    {
        for (int i = 0; i < MAX_COLLIDERS; i++)
            positions[i] = GetNewLocation(i);

        for (int i = 0; i < MAX_COLLIDERS; i++)
            AddColliderToLine(i);
    }

    private Vector3 GetNewLocation(int index)
    {
        index %= MAX_COLLIDERS;

        float height = Random.Range(3.0f, 4.0f);
        spawnAngle = 2 * Mathf.PI * (index / (float)MAX_COLLIDERS);

        float x = Mathf.Cos(spawnAngle) * height;
        float y = Mathf.Sin(spawnAngle) * height;
        return new Vector3(x, 0.0f, y);
    }

    private void AddColliderToLine(int index)
    {
        Vector3 current = positions[index];
        Vector3 prev = positions[(index + MAX_COLLIDERS - 1) % MAX_COLLIDERS];
        CapsuleCollider collider = colliders[index];

        float magnitude = Vector3.Distance(prev, current);

        Vector3 localScale = collider.transform.localScale;
        localScale.y = magnitude * 0.5f;
        collider.transform.localScale = localScale;

        collider.transform.position = (prev + current) * 0.5f;
        collider.transform.rotation = Quaternion.Euler(90.0f, (Mathf.Atan2(prev.x - current.x, prev.z - current.z) * Mathf.Rad2Deg), 0);
    }
}
