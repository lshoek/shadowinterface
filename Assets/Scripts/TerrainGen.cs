using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGen : MonoBehaviour
{

    public static int capturePoints = 46;
    public float height = 5.0f;
    public float angle;
    public float angleT;
    public bool flip;

    private LineRenderer line;
    private BoxCollider col;
    private GameObject temp;

    Vector3[] positions = new Vector3[capturePoints];
    int counter = 0;

    float lastSpawned = 0.0f;
    float spawnDelay = 0.125f;

    void Start()
    {
        LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
        BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();


    }

    void Update()
    {
        line = GetComponent<LineRenderer>();
        col = GetComponent<BoxCollider>();
        line.positionCount = capturePoints;
        line.widthMultiplier = 0.2f;

        float elapsedTime = Time.time;
        height = Random.Range(3.0f, 3.2f);
        Vector3 spawnVector = Application.Instance.GravityBody.Position + new Vector3(Mathf.Cos(elapsedTime), 0.0f, Mathf.Sin(elapsedTime)) * height;
        Debug.DrawLine(Application.Instance.GravityBody.Position, spawnVector);

        Debug.Log(spawnVector.x);

        if(spawnVector.z > 0){
            flip = false;
        }
        else{
            flip = true;
        }

        if (elapsedTime - lastSpawned > spawnDelay)
        {
            if (counter > capturePoints - 1)
            {
                counter = 0;
            }

            positions[counter] = new Vector3(spawnVector.x, spawnVector.y, spawnVector.z);
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

        //angle = (Mathf.Abs(positions[counter].z - positions[counter + 1].z) / Mathf.Abs(positions[counter].x - positions[counter + 1].x));
        //if ((positions[counter].y < positions[counter + 1].y && positions[counter].x > positions[counter + 1].x) || (positions[counter + 1].y < positions[counter].y && positions[counter + 1].x > positions[counter].x))
        //{
        //    angle *= -1;
        //}
        //if (flip)
        //{
        //    angleT = Mathf.Rad2Deg * Mathf.Atan(angle);
        //}

        //else{
        //    angleT = Mathf.Rad2Deg * Mathf.Tan(angle);
        //}
        //angleT = Mathf.Rad2Deg * Mathf.Atan2(Mathf.Abs(positions[counter].z - positions[counter + 1].z),Mathf.Abs(positions[counter].x - positions[counter + 1].x));
        //col.transform.rotation = Quaternion.Euler(0, angleT, 0);

        Vector3 src = positions[counter];
        Vector3 dst = positions[counter + 1];
        Vector3 offset = dst - src;
        Vector3 pos = src + offset * 0.5f;

        col.transform.position = pos;
        col.transform.LookAt(src);
    }

}
