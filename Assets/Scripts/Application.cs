using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Application : MonoBehaviour
{
    public static Application Instance { get; private set; }

    [HideInInspector] public PointGravity GravityBody;
    [HideInInspector] public Transform WorldParent;
    [HideInInspector] public DepthSourceManager DepthManager;
    [HideInInspector] public TerrainGenerator Generator;
    [HideInInspector] public PlayfieldWatcher Watcher;
    [HideInInspector] public ColorPalette Palette;
    [HideInInspector] public Camera OverlayCamera;

    public TextMeshPro ScoreTextMesh;
    public Animator Shoeprints;

    public enum GameState
    {
        IDLE,
        RUNNING,
        GAMEOVER
    }
    private GameState state;
    public GameState State
    {
        get { return state; }
        private set
        {
            if (value == GameState.IDLE)
            {
                Watcher.OnPlayfieldEmpty = null;
                Watcher.OnPlayfieldOccupied = delegate { StartGame(); };
            }
            else if (value == GameState.RUNNING)
            {
                Watcher.OnPlayfieldEmpty = delegate { StopGame(); };
                Watcher.OnPlayfieldOccupied = null;
            }
            else if (value == GameState.GAMEOVER)
            {
                Watcher.OnPlayfieldEmpty = delegate { Idle(); };
                Watcher.OnPlayfieldOccupied = null;
            }
            state = value;
        }
    }

    // likely getting rid of this which is good
    List<Planetoid> planetoids;

    float startTime = 0.0f;
    float survivalTime = 0.0f;

    float lastSpawned = 0.0f;
    float spawnDelay = 5.0f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        WorldParent = GameObject.FindGameObjectWithTag("WorldParent").transform;
        GravityBody = FindObjectOfType<PointGravity>();
        DepthManager = FindObjectOfType<DepthSourceManager>();
        Generator = FindObjectOfType<TerrainGenerator>();
        Watcher = FindObjectOfType<PlayfieldWatcher>();

        UnityEngine.Application.targetFrameRate = 60;
        Idle();

        ScoreTextMesh.gameObject.SetActive(false);

        GameObject ob = new GameObject("OverlayCamera");
        OverlayCamera = ob.AddComponent<Camera>();
        OverlayCamera.enabled = false;

        Palette = gameObject.AddComponent<ColorPalette>();

        CopyCamera cc = ob.AddComponent<CopyCamera>();
        cc.Initialize(Camera.main);

        OverlayCamera.cullingMask = 1 << LayerMask.NameToLayer("ShadowOverlay");

        planetoids = new List<Planetoid>();

        //StartGame();
    }

    void Update()
    {
        float elapsedTime = Time.time;
        Vector3 spawnVector = GravityBody.Position + new Vector3(Mathf.Cos(elapsedTime), 0.0f, Mathf.Sin(elapsedTime)) * 10.0f;

        if (elapsedTime - lastSpawned > spawnDelay)
        {
            if (planetoids.Count < 4)
                SpawnPlanetoid(spawnVector);
            lastSpawned = elapsedTime;
        }

        if (State == GameState.RUNNING)
        {
            survivalTime = elapsedTime - startTime;
            TimeSpan timeFormat = TimeSpan.FromSeconds(survivalTime);
            ScoreTextMesh.text = string.Format("{0:00}:{1:00}:{2:000}", timeFormat.Minutes, timeFormat.Seconds, timeFormat.Milliseconds);

            ScoreTextMesh.rectTransform.pivot = new Vector2(0.0f, -4.5f);
            ScoreTextMesh.transform.rotation = Quaternion.Euler(new Vector3(90.0f, -Generator.GetSmoothAngle() * Mathf.Rad2Deg + 90, 0.0f));
        }
    }

    private void Idle()
    {
        State = GameState.IDLE;
        ScoreTextMesh.gameObject.SetActive(false);

        Shoeprints.Play("FadeIn");
    }

    public void StartGame()
    {
        State = GameState.RUNNING;
        Generator.StartGeneration();

        ScoreTextMesh.gameObject.SetActive(true);
        startTime = Time.time;

        Shoeprints.Play("FadeOut");
    }

    public void StopGame()
    {
        State = GameState.GAMEOVER;
        Generator.StopGeneration();
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
