using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGen : MonoBehaviour
{
    
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
    private BoxCollider col;
    private GameObject temp;

    Vector3[] positions = new Vector3[capturePoints];
    int counter = 0;



    void Start()
    {
        LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
        BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
        speed = (2 * Mathf.PI) / speedMult; //2*PI in degress is 360, so you get 10 seconds to complete a circle
        spawnDelay = speedMult / capturePoints;


    }

    void Update()
    {
        line = GetComponent<LineRenderer>();
        col = GetComponent<BoxCollider>();
        line.positionCount = capturePoints;
        line.widthMultiplier = 0.1f;

        float elapsedTime = Time.time;
        height = Random.Range(3.0f, 4.2f);

        spawnAngle += speed * Time.deltaTime; //if you want to switch direction, use -= instead of +=

        x = Mathf.Cos(spawnAngle) * height;
        y = Mathf.Sin(spawnAngle) * height;
        Vector3 spawnLoc = new Vector3(x, 0.0f, y);

        Debug.Log(spawnDelay);
        Debug.DrawLine(Application.Instance.GravityBody.Position, spawnLoc);
        //Debug.Log("X: " + x + "Y: " + y);


        if (elapsedTime - lastSpawned > spawnDelay)
        {
            if (counter > capturePoints-1)
            {
                counter = 0;
            }

            positions[counter] = new Vector3(spawnLoc.x, spawnLoc.y, spawnLoc.z);
            counter++;
            AddColliderToLine();
            lastSpawned = elapsedTime;


        }

        for (int i = 0; i < capturePoints; i++)
        {
            line.SetPositions(positions);

        }
    }




    private void AddColliderToLine()
    {

        col.transform.parent = line.transform; // Collider is added as child object of line

        float lineLength = Vector3.Distance(positions[counter], positions[counter + 1]); // length of line
        col.size = new Vector3(lineLength, 0.2f, 0.2f); // size of collider is set where X is length of line, Y is width of line, Z will be set as per requirement
        Vector3 midPoint = (positions[counter] + positions[counter + 1]) / 2;
        col.transform.position = midPoint; // setting position of collider object
        col.center = new Vector3(0, 0, 0);

        // Following lines calculate the angle between startPos and endPos

        Vector3 src = positions[counter];
        Vector3 dst = positions[counter + 1];
        Vector3 offset = dst - src;
        Vector3 pos = src + offset * 0.5f;

        angle = Mathf.Atan2(positions[counter].x - positions[counter + 1].x,positions[counter].z - positions[counter + 1].z) * Mathf.Rad2Deg;

        col.transform.position = pos;
        col.transform.rotation = Quaternion.Euler(0, angle-90, 0);

    }

}
