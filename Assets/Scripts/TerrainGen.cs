using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGen : MonoBehaviour
{

    const int MAX_DEGREES = 360;
    const int MAX_COLLIDERS = 45;

    public GameObject ColliderParent;

    public static int capturePoints = 50;
    public float height = 5.0f;
    public float angle;

    float spawnAngle = 0;
    float speedMult = 10;
    float speed;
    float x, y;

    float lastSpawned = 0.0f;
    float spawnDelay;

    private LineRenderer line;
    private BoxCollider[] colliders;

    Vector3[] positions = new Vector3[MAX_COLLIDERS];
    int counter = 0;

    public Renderer rend;
    public PhysicMaterial physicMaterial;


    void Start()
    {
        LineRenderer lineRenderer = gameObject.GetComponent<LineRenderer>();
        //BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
        colliders = new BoxCollider[MAX_COLLIDERS];

        for (int i = 0; i < MAX_COLLIDERS; i++)
        {
            colliders[i] = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<BoxCollider>();
            colliders[i].transform.SetParent(ColliderParent.transform);
            rend = colliders[i].gameObject.GetComponent<Renderer>();
            rend.enabled = false;

            PhysicMaterial material = new PhysicMaterial();
            material.dynamicFriction = 0;
            material.staticFriction = 0;
            colliders[i].material = material;
        }


        //int deg = MAX_COLLIDERS / MAX_DEGREES;

        speed = (2 * Mathf.PI) / speedMult; //2*PI in degress is 360, so you get 10 seconds to complete a circle
        spawnDelay = speedMult / MAX_COLLIDERS;


    }


    void Update()
    {
        line = GetComponent<LineRenderer>();
        //colliders[counter] = GetComponent<BoxCollider>();
        line.positionCount = MAX_COLLIDERS;
        line.widthMultiplier = 0.1f;

        float elapsedTime = Time.time;
        height = Random.Range(3.0f, 3.1f);

        spawnAngle += speed * Time.deltaTime; //if you want to switch direction, use -= instead of +=

        x = Mathf.Cos(spawnAngle) * height;
        y = Mathf.Sin(spawnAngle) * height;
        Vector3 spawnLoc = new Vector3(x, 0.0f, y);

        Debug.Log(spawnDelay);
        Debug.DrawLine(Application.Instance.GravityBody.Position, spawnLoc);
        Debug.Log(counter);
        //Debug.Log("X: " + x + "Y: " + y);


        if (elapsedTime - lastSpawned > spawnDelay)
        {
            if (counter > MAX_COLLIDERS-1)
            {
                counter = 0;
            }

            positions[counter] = new Vector3(spawnLoc.x, spawnLoc.y, spawnLoc.z);
            counter++;
            AddColliderToLine();
            lastSpawned = elapsedTime;


        }

        for (int i = 0; i < MAX_COLLIDERS; i++)
        {
            line.SetPositions(positions);

        }
    }



    private void AddColliderToLine()
    {
        float lineLength;
        Vector3 midPoint;
        Vector3 src;
        Vector3 dst;

        if(counter > MAX_COLLIDERS-2){
            lineLength = Vector3.Distance(positions[counter-1], positions[0]); // length of line
            midPoint = (positions[counter-1] + positions[0]) / 2;
            src = positions[counter-1];
            dst = positions[0];
            angle = Mathf.Atan2(positions[counter-1].x - positions[0].x, positions[counter-1].z - positions[0].z) * Mathf.Rad2Deg;


        }
        else{
            lineLength = Vector3.Distance(positions[counter], positions[counter + 1]); // length of line
            midPoint = (positions[counter] + positions[counter + 1]) / 2;
            src = positions[counter];
            dst = positions[counter + 1];
            angle = Mathf.Atan2(positions[counter].x - positions[counter + 1].x, positions[counter].z - positions[counter + 1].z) * Mathf.Rad2Deg;


        }

        colliders[counter].size = new Vector3(lineLength, 0.2f, 0.2f); // size of collider is set where X is length of line, Y is width of line, Z will be set as per requirement
        colliders[counter].transform.position = midPoint; // setting position of collider object
        colliders[counter].center = new Vector3(0, 0, 0);

        // Following lines calculate the angle between startPos and endPos


        Vector3 offset = dst - src;
        Vector3 pos = src + offset * 0.5f;


        colliders[counter].transform.position = pos;
        colliders[counter].transform.rotation = Quaternion.Euler(0, angle-90, 0);

    }

}
