using UnityEngine;
using static GameManager;

public class PlayerTrigger : MonoBehaviour {

    [SerializeField] float PlayfieldTimeout = 2.0f;

    private float lastCollisionTime = 0.0f;
    private bool userInPlayfield = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        float elapsedTime = Time.time;
        if (userInPlayfield && elapsedTime - lastCollisionTime > PlayfieldTimeout)
        {
            userInPlayfield = false;
            Debug.Log("Start the game!!!!");
            //Application.Instance.StartGame();
        }
	}

    void OnTriggerEnter(Collider col)
    {
        lastCollisionTime = Time.time;
        GameState state = Application.Instance.GameManager.State;

        // don't do anything if the game is running
        if (state == GameState.RUNNING) return;

        if (!userInPlayfield && col.gameObject.name == "MeshColliderTester")
        {
            userInPlayfield = true;
            Debug.Log("HIT!!!!");
        }
    }
}
