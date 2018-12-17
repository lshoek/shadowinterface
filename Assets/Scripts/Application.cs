using Assets.Scripts;
using System.Collections.Generic;
using UnityEngine;

public class Application : MonoBehaviour
{
    public static Application Instance { get; private set; }

    public Transform WorldParent;
    public PointGravity GravityBody;
    public DepthSourceManager DepthManager;

    [HideInInspector] public Camera OverlayCamera;

    // should be a smart list implementation managing list indices and avoiding the retrieval of null references
    private List<Planetoid> planetoids;

    public delegate void OnInitializedDelegate();
    public OnInitializedDelegate OnInitialized;

    public ColorPalette Palette;

    float lastSpawned = 0.0f;
    float spawnDelay = 5.0f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        UnityEngine.Application.targetFrameRate = 60;
        WorldParent = GameObject.FindGameObjectWithTag("WorldParent").transform;
        DepthManager = FindObjectOfType<DepthSourceManager>();
        if (DepthManager == null) Debug.Log("NULL");

        GameObject ob = new GameObject("OverlayCamera");
        OverlayCamera = ob.AddComponent<Camera>();
        OverlayCamera.enabled = false;

        Palette = gameObject.AddComponent<ColorPalette>();

        CopyCamera cc = ob.AddComponent<CopyCamera>();
        cc.Initialize(Camera.main);

        OverlayCamera.cullingMask = 1 << LayerMask.NameToLayer("ShadowOverlay");

        planetoids = new List<Planetoid>();

        if (OnInitialized != null)
            OnInitialized();
    }

    void Update()
    {
        if (GravityBody != null)
        {
            float elapsedTime = Time.time;
            Vector3 spawnVector = GravityBody.Position + new Vector3(Mathf.Cos(elapsedTime), 0.0f, Mathf.Sin(elapsedTime)) * 10.0f;

            if (elapsedTime - lastSpawned > spawnDelay)
            {
                if (planetoids.Count < 0)
                    SpawnPlanetoid(spawnVector);
                lastSpawned = elapsedTime;
            }
            //Debug.DrawLine(GravityBody.Position, spawnVector);
        }
    }

    #region "Planetoid Wrapper Methods"
    void SpawnPlanetoid(Vector3 position)
    {
        GameObject ob = Instantiate(Resources.Load("Prefabs/Planetoid") as GameObject);
        ob.transform.SetParent(WorldParent);
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

    void DespawnPlanetoids()
    {
        foreach (Planetoid p in planetoids)
            DespawnPlanetoid(p);
    }
    #endregion
}
