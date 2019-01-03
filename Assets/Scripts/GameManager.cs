using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    [HideInInspector] public PointGravity GravityBody;
    //[HideInInspector] public TerrainGenerator Generator;
    [HideInInspector] public PlayfieldWatcher Watcher;

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
                Watcher.DisableOnPlayfieldEmpty();
                Watcher.OnPlayfieldOccupied += StartGame;
            }
            else if (value == GameState.RUNNING)
            {
                Watcher.OnPlayfieldEmpty += StopGame;
                Watcher.DisableOnPlayfieldOccupied();
            }
            else if (value == GameState.GAMEOVER)
            {
                Watcher.OnPlayfieldEmpty += Idle;
                Watcher.DisableOnPlayfieldOccupied();
            }
            state = value;
        }
    }

    [SerializeField] [Range(0.0f, 10.0f)] float noiseMultiplier = 1.5f;

    const float spawnVectorMagnitude = 20.0f;

    List<Planetoid> planetoids;

    float startTime = 0.0f;
    float survivalTime = 0.0f;

    float lastSpawned = 0.0f;
    float initialSpawnInterval = 5.0f;
    float spawnInterval;

    Vector2 direction;
    Vector2 noiseStep;

    public TextMeshPro InfoTextMesh;
    public TextMeshPro ScoreTextMesh;
    public Animator Shoeprints;

    void Awake()
    {
        GravityBody = FindObjectOfType<PointGravity>();
        //Generator = FindObjectOfType<TerrainGenerator>();
        Watcher = FindObjectOfType<PlayfieldWatcher>();

        planetoids = new List<Planetoid>();
        Idle();
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
        //Generator.StartGeneration();

        ScoreTextMesh.gameObject.SetActive(true);
        startTime = Time.time;

        // random seed
        float randomAlpha = Random.value * Mathf.PI * 2f;
        direction = new Vector2(Mathf.Cos(randomAlpha), Mathf.Sin(randomAlpha));

        spawnInterval = initialSpawnInterval;

        Shoeprints.Play("FadeOut");
    }

    public void StopGame()
    {
        State = GameState.GAMEOVER;
        //Generator.StopGeneration();
    }

    void Update()
    {
        float elapsedTime = Time.time;
        float intensityRamp = (elapsedTime / 100.0f) + 1.0f;

        noiseStep = direction * elapsedTime * noiseMultiplier;
        float turb = Mathf.PerlinNoise(noiseStep.x, noiseStep.y);
        float noisyAmplitude = (elapsedTime + 10.0f * turb) * intensityRamp;

        Vector3 spawnVector = GravityBody.Position + new Vector3(Mathf.Cos(noisyAmplitude), 0.0f,Mathf.Sin(noisyAmplitude)) * spawnVectorMagnitude;
        Debug.DrawLine(GravityBody.Position, spawnVector);

        if (State == GameState.RUNNING)
        {
            // planetoids
            if (elapsedTime - lastSpawned > spawnInterval)
            {
                SpawnPlanetoid(spawnVector, Random.Range(1.0f, 1.5f), Random.Range(0.75f, 2.0f));
                spawnInterval = initialSpawnInterval / (intensityRamp + Mathf.Abs(turb) * 3.0f);
                lastSpawned = elapsedTime;
            }
            GravityBody.UpdateSubjects(planetoids);

            foreach (Planetoid p in planetoids)
            {
                p.Material.SetColor("_Color", Color.HSVToRGB(turb, 0.333f, 1.0f));
            }

            // hud
            survivalTime = elapsedTime - startTime;
            TimeSpan timeFormat = TimeSpan.FromSeconds(survivalTime);

            ScoreTextMesh.text = string.Format("{0:00}:{1:00}:{2:000}", timeFormat.Minutes, timeFormat.Seconds, timeFormat.Milliseconds);
            InfoTextMesh.text = string.Format("intensity: {0}", intensityRamp);

            //ScoreTextMesh.rectTransform.pivot = new Vector2(0.0f, -4.5f);
            //ScoreTextMesh.transform.rotation = Quaternion.Euler(new Vector3(90.0f, -Generator.GetSmoothAngle() * Mathf.Rad2Deg + 90, 0.0f));
        }
    }

    #region "Planetoid Wrapper Methods"
    private void SpawnPlanetoid(Vector3 position, float mass, float size)
    {
        GameObject ob = Instantiate(Resources.Load("Prefabs/Planetoid") as GameObject);
        ob.transform.SetParent(Application.Instance.WorldParent);
        ob.name = string.Format("Planetoid{0}", planetoids.Count);
        ob.transform.position = position;

        Planetoid p = ob.GetComponent<Planetoid>();
        p.MultiplyMass(mass);
        p.MultiplySize(size);

        planetoids.Add(p);
    }

    private void DespawnPlanetoid(Planetoid p)
    {
        planetoids.Remove(p);
        Destroy(p.gameObject);
    }

    private void DespawnPlanetoids()
    {
        foreach (Planetoid p in planetoids)
            DespawnPlanetoid(p);
    }
    #endregion
}
