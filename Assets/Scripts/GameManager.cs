using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

    [SerializeField] private bool EnableGame = true;

    [SerializeField] [Range(0.0f, 10.0f)] float noiseMultiplier = 1.5f;
    [SerializeField] [Range(0.0f, 1.0f)] float friendlyPlanetoidRate = 0.1f;

    const float spawnVectorMagnitude = 20.0f;

    List<Planetoid> planetoids;
    List<Planetoid> planetoidsToDespawn;

    float elapsedTime = 0.0f;
    float startTime = 0.0f;
    float survivalTime = 0.0f;
    double bestScore = 0;

    float lastSpawned = 0.0f;
    float initialSpawnInterval = 5.0f;
    float spawnInterval;

    public int Dinosaurs = 0;
    bool startup = true;

    Vector2 direction;
    Vector2 noiseStep;

    [SerializeField] Text InstructionsText;
    [SerializeField] Text BestText;
    [SerializeField] Text ScoreText;
    [SerializeField] Text DinoScoreText;
    [SerializeField] Text GameOverText;
    [SerializeField] Text IntroText;

    [SerializeField] Animator TitleAnimator;
    [SerializeField] Animator ShoeprintsAnimator;
    [SerializeField] Animator DinoHeadAnimator;

    void Awake()
    {
        GravityBody = FindObjectOfType<PointGravity>();
        Watcher = FindObjectOfType<PlayfieldWatcher>();

        planetoids = new List<Planetoid>();
        planetoidsToDespawn = new List<Planetoid>();

        ScoreText.GetComponent<Animator>().Play("Off");
        DinoScoreText.GetComponent<Animator>().Play("Off");
        DinoHeadAnimator.GetComponent<Animator>().Play("Off");

        Idle();
    }

    private void Idle()
    {
        State = GameState.IDLE;

        DespawnAllPlanetoids();
        foreach (GameObject ob in GameObject.FindGameObjectsWithTag("X")) Destroy(ob);

        if (!startup)
        {
            GameOverText.GetComponent<Animator>().Play("FadeOut");
            InstructionsText.GetComponent<Animator>().Play("FadeIn");
            ScoreText.GetComponent<Animator>().Play("FadeOut");
            DinoScoreText.GetComponent<Animator>().Play("FadeOut");

            DinoHeadAnimator.GetComponent<Animator>().Play("FadeOut");
            ShoeprintsAnimator.Play("FadeIn");
            TitleAnimator.Play("FadeIn");

            InstructionsText.text = "step inside to play";
        }
        else GameOverText.GetComponent<Animator>().Play("Off");
        startup = false;
    }

    public void StartGame()
    {
        if (!EnableGame) return;

        State = GameState.RUNNING;

        startTime = Time.time;
        elapsedTime = 0.0f;
        Dinosaurs = 0;

        // random seed
        float randomAlpha = Random.value * Mathf.PI * 2f;
        direction = new Vector2(Mathf.Cos(randomAlpha), Mathf.Sin(randomAlpha));

        spawnInterval = initialSpawnInterval;

        InstructionsText.GetComponent<Animator>().Play("FadeOut");
        ScoreText.GetComponent<Animator>().Play("FadeIn");
        DinoScoreText.GetComponent<Animator>().Play("FadeIn");
        IntroText.GetComponent<Animator>().Play("Show");

        DinoHeadAnimator.Play("FadeIn");
        ShoeprintsAnimator.Play("FadeOut");
        TitleAnimator.Play("FadeOut");

        InstructionsText.text = string.Format("deflect hostile aircraft. save the dinosaurs.");
    }

    public void StopGame()
    {
        State = GameState.GAMEOVER;
        TimeSpan timeFormat = TimeSpan.FromSeconds(survivalTime);
        double score = (int)timeFormat.TotalSeconds*10 + Dinosaurs*100;

        bool newRecord = false;
        if (score > bestScore)
        {
            bestScore = score;
            BestText.text = string.Format("best {0}", bestScore);
            newRecord = true;
        }
        ScoreText.text = string.Format("{0:00}:{1:00}:{2:000} = {3} + {4} dinos x 100 = {5}", 
            timeFormat.Minutes, timeFormat.Seconds, timeFormat.Milliseconds,
            (int)timeFormat.TotalSeconds*10, Dinosaurs, score);

        InstructionsText.text = newRecord ? "new record. please step out and wait" : "please step out and wait for title screen";

        GameOverText.GetComponent<Animator>().Play("FadeIn");
        InstructionsText.GetComponent<Animator>().Play("FadeIn");

        if (!Watcher.UserInPlayField)
            Idle();
    }

    void Update()
    {
        elapsedTime = Time.time;
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
                    SpawnPlanetoid(PlanetoidType.HOSTILE, spawnVector, Random.Range(0.75f, 1.5f)/intensityRamp, Random.Range(2.0f, 3.0f));
                else
                    SpawnPlanetoid(PlanetoidType.FRIENDLY, spawnVector, Random.Range(0.75f, 1.25f)/intensityRamp, 1.0f);

                spawnInterval = initialSpawnInterval / (intensityRamp + Mathf.Abs(Turb) * 3.0f);
                lastSpawned = elapsedTime;
            }
            GravityBody.UpdateSubjects(planetoids);

            // hud
            survivalTime = elapsedTime - startTime;
            TimeSpan timeFormat = TimeSpan.FromSeconds(survivalTime);

            ScoreText.text = string.Format("{0:00}:{1:00}:{2:000}", timeFormat.Minutes, timeFormat.Seconds, timeFormat.Milliseconds);
            DinoScoreText.text = string.Format("{0:000}", Dinosaurs);

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

        ob.name = (type == PlanetoidType.HOSTILE) ? "ship" : "dino";
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
