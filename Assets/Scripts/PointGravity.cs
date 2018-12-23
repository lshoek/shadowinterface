using UnityEngine;
using System.Collections.Generic;

public class PointGravity : MonoBehaviour
{
    public float range = 10f;
    Rigidbody mainRb;

    public Collider[] Colliders { get; private set; }

    public Vector3 Position { get { return mainRb.position; } }

    void Start()
    {
        mainRb = GetComponent<Rigidbody>();
        RefreshColliders();
    }

    void Update()
    {
        RefreshColliders(); // quick and dirty for testing purposes
        foreach (Collider c in Colliders)
        {
            Rigidbody rb = c.attachedRigidbody;
            if (rb != null && rb != mainRb)
            {
                Vector3 offset = transform.position - c.transform.position;
                Vector3 dir = offset / offset.sqrMagnitude * mainRb.mass * 2.0f;
<<<<<<< HEAD

                if (!float.IsNaN(dir.x) | !float.IsNaN(dir.y) | !float.IsNaN(dir.z))
                {
                    rb.AddForce(dir);
                }
=======
                rb.AddForce(dir);
>>>>>>> 4764cb3f73e2a0781890c4fcb47be05a8d1471f6
            }
        }
    }

    public void RefreshColliders()
    {
        // note: inefficient. lock this collection on this call at some point + do not update every fixedupdate
        Colliders = Physics.OverlapSphere(transform.position, range);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}