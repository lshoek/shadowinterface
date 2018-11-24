using Assets.Scripts;
using System.Collections.Generic;
using UnityEngine;

public class Application : MonoBehaviour
{
    public static Application Instance { get; private set; }

    public PointGravity GravityBody;

    // should be a smart list implementation managing list indices and avoiding the retrieval of null references
    public List<Planetoid> planetoids;

    public Camera OverlayCamera;

    float lastSpawned = 0.0f;
    float spawnDelay = 1.0f;

    void Start ()
    {
        TryFullScreen();

        if (Instance == null)
            Instance = this;

        GameObject ob = new GameObject("OverlayCamera");
        OverlayCamera = ob.AddComponent<Camera>();
        OverlayCamera.enabled = false;

        CopyCamera cc = ob.AddComponent<CopyCamera>();
        cc.Initialize(Camera.main);

        OverlayCamera.cullingMask = 1 << LayerMask.NameToLayer("KinectBody");

        planetoids = new List<Planetoid>();
    }
	
	void Update ()
    {
        if (GravityBody != null)
        {
            float elapsedTime = Time.time;
            Vector3 spawnVector = GravityBody.Position + new Vector3(Mathf.Cos(elapsedTime), 0.0f, Mathf.Sin(elapsedTime)) * 10.0f;

            if (elapsedTime - lastSpawned > spawnDelay)
            {
                if (planetoids.Count < 6)
                    SpawnPlanetoid(spawnVector);
                lastSpawned = elapsedTime;
            }
            Debug.DrawLine(GravityBody.Position, spawnVector);
        }
    }

    void TryFullScreen()
    {
        // external display (executable only)
        if (Display.displays.Length > 1)
        {
            Display.displays[1].Activate();
            Screen.fullScreen = true;
        }
    }

    #region "Planetoid Wrapper Methods"
    void SpawnPlanetoid(Vector3 position)
    {
        GameObject ob = Instantiate(Resources.Load("Prefabs/Planetoid") as GameObject);
        ob.name = string.Format("Planetoid{0}", planetoids.Count);
        ob.transform.position = position;

        // given the prefab this should never fail
        Planetoid poid = ob.GetComponent<Planetoid>();
        planetoids.Add(poid);
        GravityBody.RefreshColliders();
    }

    void DespawnPlanetoid(Planetoid poid)
    {
        planetoids.Remove(poid);
        Destroy(poid.gameObject);
    }
    #endregion
}
