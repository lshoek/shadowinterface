using System;
using UnityEngine;
using static GameManager;

public class PlayfieldWatcher : MonoBehaviour
{
    [SerializeField] float PlayfieldTimeout = 8.0f;

    public event Action OnPlayfieldOccupied;
    public event Action OnPlayfieldEmpty;

    public bool UserInPlayField { get; private set; } = false;

    private float lastCollisionTime = 0.0f;
    private BoxCollider boxCollider;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    void Update()
    {
        float elapsedTime = Time.time;
        if (UserInPlayField && elapsedTime - lastCollisionTime > PlayfieldTimeout)
        {
            UserInPlayField = false;
            if (OnPlayfieldEmpty != null) OnPlayfieldEmpty.Invoke();
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.name == "ColliderMesh")
        {
            lastCollisionTime = Time.time;

            // don't do anything if the game is running
            if (Application.Instance.GameManager.State == GameState.RUNNING) return;

            if (!UserInPlayField)
            {
                if (OnPlayfieldOccupied != null) OnPlayfieldOccupied.Invoke();
                UserInPlayField = true;
            }
        }
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
