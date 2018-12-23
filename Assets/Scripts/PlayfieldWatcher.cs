using System;
using UnityEngine;

public class PlayfieldWatcher : MonoBehaviour
{
    [SerializeField] float PlayfieldTimeout = 2.0f;

    public delegate void PlayfieldOccupiedDelegate();
    public PlayfieldOccupiedDelegate OnPlayfieldOccupied;

    public delegate void PlayfieldEmptyDelegate();
    public PlayfieldEmptyDelegate OnPlayfieldEmpty;

    private float lastCollisionTime = 0.0f;
    private bool userInPlayfield = false;

    void Update()
    {
        float elapsedTime = Time.time;
        if (userInPlayfield && elapsedTime - lastCollisionTime > PlayfieldTimeout)
        {
            if (OnPlayfieldEmpty != null) OnPlayfieldEmpty.Invoke();
            userInPlayfield = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("HEY");
        lastCollisionTime = Time.time;
        Application.GameState state = Application.Instance.State;

        // don't do anything if the game is running
        if (state == Application.GameState.RUNNING) return;

        if (!userInPlayfield && UserInPlayfield(collision))
        {
            if (OnPlayfieldEmpty != null) OnPlayfieldOccupied.Invoke();
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
