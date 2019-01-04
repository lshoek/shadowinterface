using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Planetoid;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    [HideInInspector] public PointGravity GravityBody;
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
    public float Turb { get; private set; }

    [SerializeField] [Range(0.0f, 10.0f)] float noiseMultiplier = 1.5f;
    [SerializeField] [Range(0.0f, 1.0f)] float friendlyPlanetoidRate = 0.1f;
    [SerializeField] private float hostileHue;
    [SerializeField] private float friendlyHue;

    public float HostileHue { get { return hostileHue; } }
    public float FriendlyHue{ get { return friendlyHue; } }

    const float spawnVectorMagnitude = 20.0f;

    List<Planetoid> planetoids;
    List<Planetoid> planetoidsToDespawn;

    float startTime = 0.0f;
    float survivalTime = 0.0f;
    float bestSurvivalTime = 0.0f;

    float lastSpawned = 0.0f;
    float initialSpawnInterval = 5.0f;
    float spawnInterval;

    Vector2 direction;
    Vector2 noiseStep;
    public TextMeshPro InfoTextMesh;
    public TextMeshPro BestScoreTextMesh;
    public TextMeshPro ScoreTextMesh;
    public Animator Shoeprints;

    void Awake()
    {
        GravityBody = FindObjectOfType<PointGravity>();
        Watcher = FindObjectOfType<PlayfieldWatcher>();

        planetoids = new List<Planetoid>();
        planetoidsToDespawn = new List<Planetoid>();
        Idle();
    }

    private void Idle()
    {
        State = GameState.IDLE;

        DespawnAllPlanetoids();
        foreach (GameObject ob in GameObject.FindGameObjectsWithTag("X")) Destroy(ob);

        ScoreTextMesh.gameObject.SetActive(false);
        Shoeprints.Play("FadeIn");

        InfoTextMesh.text = "step inside to start a new round";
    }

    public void StartGame()
    {
        State = GameState.RUNNING;

        ScoreTextMesh.gameObject.SetActive(true);
        startTime = Time.time;

        // random seed
        float randomAlpha = Random.value * Mathf.PI * 2f;
        direction = new Vector2(Mathf.Cos(randomAlpha), Mathf.Sin(randomAlpha));

        spawnInterval = initialSpawnInterval;

        Shoeprints.Play("FadeOut");
        InfoTextMesh.text = string.Format("survival mode");
    }

    public void StopGame()
    {
        State = GameState.GAMEOVER;

        string gameOverString;
        bool newRecord = false;
        if (survivalTime > bestSurvivalTime)
        {
            bestSurvivalTime = survivalTime;
            TimeSpan timeFormat = TimeSpan.FromSeconds(survivalTime);
            BestScoreTextMesh.text = string.Format("best {0:00}:{1:00}:{2:000}", timeFormat.Minutes, timeFormat.Seconds, timeFormat.Milliseconds);
            newRecord = true;
        }
        gameOverString = newRecord ? "game over. new record." : "game over.";
        InfoTextMesh.text = string.Format("{0} step out and wait for the shoeprints", gameOverString);

        if (!Watcher.UserInPlayField)
            Idle();
    }

    void Update()
    {
        float elapsedTime = Time.time;
        float intensityRamp = (elapsedTime / 250.0f) + 1.0f;

        noiseStep = direction * elapsedTime * noiseMultiplier;
        Turb = Mathf.PerlinNoise(noiseStep.x, noiseStep.y);
        float noisyAmplitude = (elapsedTime + 10.0f * Turb) * intensityRamp;

        float cosine = Mathf.Cos(noisyAmplitude);
        float sine = Mathf.Abs(Mathf.Sin(noisyAmplitude));

        Vector3 spawnVector = GravityBody.Position + new Vector3(cosine, 0.0f, sine) * spawnVectorMagnitude;
        Debug.DrawLine(GravityBody.Position, spawnVector);

        if (State == GameState.RUNNING)
        {
            // planetoids
            HandleCollisions();

            if (elapsedTime - lastSpawned > spawnInterval)
            {
                if (Random.Range(0.0f, 1.0f) > friendlyPlanetoidRate)
                    SpawnPlanetoid(PlanetoidType.HOSTILE, spawnVector, Random.Range(1.0f, 1.5f), Random.Range(0.75f, 2.0f));
                else
                    SpawnPlanetoid(PlanetoidType.FRIENDLY, spawnVector, Random.Range(1.0f, 1.5f), Random.Range(0.75f, 2.0f));

                spawnInterval = initialSpawnInterval / (intensityRamp + Mathf.Abs(Turb) * 3.0f);
                lastSpawned = elapsedTime;
            }
            GravityBody.UpdateSubjects(planetoids);

            // hud
            survivalTime = elapsedTime - startTime;
            TimeSpan timeFormat = TimeSpan.FromSeconds(survivalTime);

            ScoreTextMesh.text = string.Format("{0:00}:{1:00}:{2:000}", timeFormat.Minutes, timeFormat.Seconds, timeFormat.Milliseconds);

            //ScoreTextMesh.rectTransform.pivot = new Vector2(0.0f, -4.5f);
            //ScoreTextMesh.transform.rotation = Quaternion.Euler(new Vector3(90.0f, -Generator.GetSmoothAngle() * Mathf.Rad2Deg + 90, 0.0f));
        }
    }

    public void SpawnCross(Vector3 position)
    {
        GameObject ob = Instantiate(Resources.Load("Prefabs/CrossMark") as GameObject);
        ob.transform.position = position;
    }

    public void AddCollision(Planetoid poid)
    {
        bool wasAdded = false;
        foreach (Planetoid p in planetoidsToDespawn)
        {
            if (p == poid)
            {
                wasAdded = true;
                break;
            }
        }
        if (!wasAdded) planetoidsToDespawn.Add(poid);
    }

    private void HandleCollisions()
    {
        foreach (Planetoid p in planetoidsToDespawn)
        {
            DespawnPlanetoid(p);
        }
        planetoidsToDespawn.Clear();
    }

    private void SpawnPlanetoid(PlanetoidType type, Vector3 position, float drag, float size)
    {
        GameObject ob = (type == PlanetoidType.HOSTILE) ? 
            Instantiate(Resources.Load("Prefabs/Ship") as GameObject) : 
            Instantiate(Resources.Load("Prefabs/Planetoid") as GameObject);

        ob.name = (type == PlanetoidType.HOSTILE) ? "ship" : "poid";
        ob.transform.SetParent(Application.Instance.WorldParent);
        ob.transform.position = position;

        if (ob != null)
        {
            Planetoid p = ob.GetComponent<Planetoid>();
            p.Manager = this;
            p.MultiplyDrag(drag);
            p.MultiplySize(size);

            planetoids.Add(p);
        }
    }

    private void DespawnPlanetoid(Planetoid p)
    {
        planetoids.Remove(p);
        Destroy(p.gameObject);
    }

    private void DespawnAllPlanetoids()
    {
        planetoidsToDespawn.Clear();
        foreach (Planetoid p in planetoids)
            Destroy(p.gameObject);
        planetoids.Clear();
    }
}
