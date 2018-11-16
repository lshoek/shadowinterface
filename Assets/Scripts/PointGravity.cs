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

    void FixedUpdate()
    {
        foreach (Collider c in Colliders)
        {
            Rigidbody rb = c.attachedRigidbody;
            if (rb != null && rb != mainRb)
            {
                Vector3 offset = transform.position - c.transform.position;
                Vector3 dir = offset / offset.sqrMagnitude * mainRb.mass * 2.0f;
                Vector3 perpdir = Quaternion.AngleAxis(-90, Vector3.down) * Vector3.Normalize(dir) * 8.0f;

                rb.AddForce(dir);
                //rb.AddForce(perpdir);

                Debug.DrawLine(c.transform.position, c.transform.position + dir * 0.1f);
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