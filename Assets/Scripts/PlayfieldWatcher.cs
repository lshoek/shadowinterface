using UnityEngine;

public class PlayfieldWatcher : MonoBehaviour
{
    [SerializeField] float PlayfieldTimeout = 2.0f;

    public delegate void OnPlayfieldOccupiedDelegate();
    public OnPlayfieldOccupiedDelegate OnPlayfieldOccupied;

    public delegate void OnPlayfieldEmptyDelegate();
    public OnPlayfieldEmptyDelegate OnPlayfieldEmpty;

    private float lastCollisionTime = 0.0f;
    private bool userInPlayfield = false;

    void Update()
    {
        if (userInPlayfield && Time.time - lastCollisionTime > PlayfieldTimeout)
        {
            OnPlayfieldEmpty();
            userInPlayfield = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        lastCollisionTime = Time.time;
        Application.GameState state = Application.Instance.State;

        // don't do anything if the game is running
        if (state == Application.GameState.RUNNING) return;

        if (UserInPlayfield(collision))
        {
            OnPlayfieldOccupied();
            userInPlayfield = true;
        }
    }

    private bool UserInPlayfield(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
            return contact.otherCollider is MeshCollider;
        return false;
    }
}
