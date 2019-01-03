using UnityEngine;

public class Rover : MonoBehaviour {

    [SerializeField] float speed = 100f;
    [SerializeField] float maxSpeed = 1f;

    Rigidbody roverRb;

    public Vector3 Position { get { return roverRb.position; } }

    void Start()
    {
        roverRb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 offset = Application.Instance.GameManager.GravityBody.Position - transform.position;
        Vector3 dir = offset / offset.sqrMagnitude * roverRb.mass * 2.0f;
        Vector3 perpdir = Quaternion.AngleAxis(90, Vector3.down) * Vector3.Normalize(dir) * speed;

        roverRb.AddForce(-perpdir);
        roverRb.velocity = Vector3.ClampMagnitude(roverRb.velocity, maxSpeed);
    }
}
