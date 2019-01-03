using System;
using UnityEngine;
using static GameManager;

public class PlayfieldWatcher : MonoBehaviour
{
    [SerializeField] float PlayfieldTimeout = 2.0f;

    public event Action OnPlayfieldOccupied;
    public event Action OnPlayfieldEmpty;

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
        lastCollisionTime = Time.time;
        GameState state = Application.Instance.GameManager.State;

        // don't do anything if the game is running
        if (state == GameState.RUNNING) return;

        if (!userInPlayfield && collision.gameObject.name == "MeshColliderTester")
        {
            if (OnPlayfieldEmpty != null) OnPlayfieldOccupied.Invoke();
            userInPlayfield = true;
            Debug.Log("HIT!!!!");

        }
    }

    private bool UserInPlayfield(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
            return contact.otherCollider is MeshCollider;
        return false;
    }

    public void DisableOnPlayfieldOccupied()
    {
        OnPlayfieldOccupied = null;
    }

    public void DisableOnPlayfieldEmpty()
    {
        OnPlayfieldEmpty = null;
    }
}
