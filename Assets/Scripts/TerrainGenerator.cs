using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField] float terrainWidth = 0.3f;
    [SerializeField] [Range(1.0f, 30.0f)] float circulationTime = 10.0f;
    [SerializeField] [Range(0.0f, 10.0f)] float noiseMultiplier = 1.5f;
    [SerializeField] [Range(0.0f, 1.0f)] float terrainSize = 0.33f;
    [SerializeField] [Range(0.0f, 10.0f)] float minHeight = 3.0f;
    [SerializeField] [Range(0.0f, 10.0f)] float maxHeight = 3.5f;

    Transform BoneParent;
    Transform JointParent;

    const int NUM_POINTS = 90;

    float timeLastSpawned = 0.0f;
    float spawnTimeStep;

    CapsuleCollider[] bones;
    SphereCollider[] joints;
    Vector3[] positions = new Vector3[NUM_POINTS];

    Vector2 direction;
    Vector2 noiseStep;

    float highPrecisionPosition = 0;
    int positionCounter = 0;
    int prevPositionCounter = 0;

    void Start()
    {
        BoneParent = GameObject.FindGameObjectWithTag("BoneParent").transform;
        JointParent = GameObject.FindGameObjectWithTag("JointParent").transform;

        bones = new CapsuleCollider[NUM_POINTS];
        joints = new SphereCollider[NUM_POINTS];

        float randomAlpha = Random.value * Mathf.PI * 2f;
        direction = new Vector2(Mathf.Cos(randomAlpha), Mathf.Sin(randomAlpha));

        for (int i = 0; i < NUM_POINTS; i++)
        {
            bones[i] = InitBone();
            joints[i] = InitJoint();
        }
        InitTerrain();
    }

    void Update()
    {
        float elapsedTime = Time.time;
        noiseStep = direction * elapsedTime * noiseMultiplier;

        highPrecisionPosition = elapsedTime % circulationTime / circulationTime;
        positionCounter = (int)Mathf.Lerp(0, NUM_POINTS, highPrecisionPosition);
       
        if (positionCounter != prevPositionCounter)
        {
            positionCounter %= NUM_POINTS;
            positions[positionCounter] = GetNewLocation(positionCounter);

            UpdateTerrainSegment(positionCounter);
            timeLastSpawned = elapsedTime;

            int numVisiblePoints = (int)(NUM_POINTS * terrainSize);
            int leftBound = (positionCounter - numVisiblePoints + NUM_POINTS) % NUM_POINTS;
            int leftBoundEdge = (positionCounter + 1 - numVisiblePoints + NUM_POINTS) % NUM_POINTS;

            for (int i = 0; i < NUM_POINTS; i++)
            {
                bool b = (positionCounter < numVisiblePoints) ? (i > leftBound | i < positionCounter) : (i < positionCounter && i > leftBound);
                bones[i].gameObject.SetActive(b);
                joints[i].gameObject.SetActive(b);
            }
            bones[leftBoundEdge].gameObject.SetActive(false);

            prevPositionCounter = positionCounter;
        }
    }
    private SphereCollider InitJoint()
    {
        SphereCollider joint = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<SphereCollider>();

        joint.transform.SetParent(JointParent);
        joint.transform.localScale *= terrainWidth;
        joint.material = Resources.Load("Resources/Materials/Physic/TerrainPhysicMaterial") as PhysicMaterial;
        joint.center = Vector3.zero;
        return joint;
    }

    private CapsuleCollider InitBone()
    {
        CapsuleCollider bone = GameObject.CreatePrimitive(PrimitiveType.Cylinder).GetComponent<CapsuleCollider>();

        bone.transform.SetParent(BoneParent);
        bone.transform.localScale *= terrainWidth;
        bone.material = Resources.Load("Resources/Materials/Physic/TerrainPhysicMaterial") as PhysicMaterial;
        bone.center = Vector3.zero;
        return bone;
    }

    private void InitTerrain()
    {
        for (int i = 0; i < NUM_POINTS; i++)
            positions[i] = GetNewLocation(i);

        for (int i = 0; i < NUM_POINTS; i++)
            UpdateTerrainSegment(i);
    }

    private Vector3 GetNewLocation(int index)
    {
        index %= NUM_POINTS;

        float turb = Mathf.PerlinNoise(noiseStep.x, noiseStep.y);
        float height = Mathf.Lerp(minHeight, maxHeight, turb);

        float spawnAngle = 2 * Mathf.PI * (index / (float)NUM_POINTS);

        float x = Mathf.Cos(spawnAngle) * height;
        float y = Mathf.Sin(spawnAngle) * height;
        return new Vector3(x, 0.0f, y);
    }

    private void UpdateTerrainSegment(int index)
    {
        Vector3 current = positions[index];
        Vector3 prev = positions[(index + NUM_POINTS - 1) % NUM_POINTS];

        CapsuleCollider bone = bones[index];
        SphereCollider joint = joints[index];

        float magnitude = Vector3.Distance(prev, current);

        Vector3 localScale = bone.transform.localScale;
        localScale.y = magnitude * 0.5f;
        bone.transform.localScale = localScale;
        bone.transform.position = (prev + current) * 0.5f;
        bone.transform.rotation = Quaternion.Euler(90.0f, (Mathf.Atan2(prev.x - current.x, prev.z - current.z) * Mathf.Rad2Deg), 0);

        joint.transform.position = current;
    }

    public float GetSmoothAngle()
    {
        return 2 * Mathf.PI * highPrecisionPosition;
    }
}
