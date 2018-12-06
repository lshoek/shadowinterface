using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGen : MonoBehaviour {
    
    public static int capturePoints = 46;
    public float height = 5.0f;

    private LineRenderer line;
    private BoxCollider col;

    Vector3[] positions = new Vector3[capturePoints];
    int counter = 0;

    float lastSpawned = 0.0f;
    float spawnDelay = 0.125f;

	void Start () {
        LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
        BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();

	}

	void Update () {
        line = GetComponent<LineRenderer>();
        col = GetComponent<BoxCollider>();
        line.positionCount = capturePoints;
        line.widthMultiplier = 0.2f;



        float elapsedTime = Time.time;
        height = Random.Range(2.0f,4.0f);
        Vector3 spawnVector = Application.Instance.GravityBody.Position + new Vector3(Mathf.Cos(elapsedTime), 0.0f, Mathf.Sin(elapsedTime)) * height;
        Debug.DrawLine(Application.Instance.GravityBody.Position, spawnVector);




        if (elapsedTime - lastSpawned > spawnDelay)
        {
            if (counter > capturePoints - 1)
            {
                counter = 0;
            }
            positions[counter] = new Vector3(spawnVector.x,spawnVector.y,spawnVector.z);
            counter++;
            AddColliderToLine();
            //Debug.Log(positions[0]);
            lastSpawned = elapsedTime;
        }

        for (int i = 0; i < capturePoints - 1; i++)
        {
           Debug.DrawLine(positions[4], positions[5]);
            line.SetPositions(positions);

        }
	}

    private void AddColliderToLine()
    {
        col.transform.parent = line.transform; // Collider is added as child object of line

        float lineLength = Vector3.Distance(positions[4], positions[5]); // length of line
        col.size = new Vector3(lineLength, 0.5f, 0.5f); // size of collider is set where X is length of line, Y is width of line, Z will be set as per requirement
        Vector3 midPoint = (positions[4] + positions[5]) / 2;
        col.transform.position = midPoint; // setting position of collider object
        // Following lines calculate the angle between startPos and endPos
        float angle = (Mathf.Abs(positions[4].y - positions[4].y) / Mathf.Abs(positions[4].x - positions[5].x));
        if ((positions[4].y < positions[5].y && positions[4].x > positions[5].x) || (positions[5].y < positions[4].y && positions[5].x > positions[4].x))
        {
            angle *= -1;
        }
        angle = Mathf.Rad2Deg * Mathf.Atan(angle);
        col.transform.Rotate(angle, 0, 0);
    }

}
