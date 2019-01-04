using UnityEngine;

public class Explosion : MonoBehaviour
{
    void OnExplosionAnimationFinished()
    {
        Destroy(gameObject);
    }
}
