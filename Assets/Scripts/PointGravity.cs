using UnityEngine;
using System.Collections.Generic;

public class PointGravity : MonoBehaviour
{
    public float AttractiveForce = 2.0f;

    public Vector3 Position { get { return mainRb.position; } }

    private Rigidbody mainRb;

    void Start()
    {
        mainRb = GetComponent<Rigidbody>();
    }

    public void UpdateSubjects(List<Planetoid> planetoids)
    {
        foreach (Planetoid p in planetoids)
        {
            Rigidbody rb = p.Collider.attachedRigidbody;
            if (rb != null && rb != mainRb)
            {
                Vector3 offset = transform.position - p.Collider.transform.position;
                Vector3 dir = offset / offset.sqrMagnitude * mainRb.mass * AttractiveForce;

                rb.AddForce(dir);
            }
        }
    }
}