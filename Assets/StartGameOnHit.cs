using UnityEngine;

public class StartGameOnHit : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (!Application.Instance.IsRunning())
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.otherCollider is MeshCollider) Application.Instance.StartGame();
            }
        }
    }
}
