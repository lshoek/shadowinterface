using System.Collections.Generic;
using UnityEngine;

public class Application : MonoBehaviour
{
    public static Application Instance { get; private set; }

    public PointGravity GravityBody;

    public List<Planetoid> planetoids;

    float lastSpawned = 0.0f;
    float spawnDelay = 0.75f;

    void Start ()
    {
        if (Instance != null)
            Instance = this;

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
                SpawnPlanetoid(spawnVector);
                lastSpawned = elapsedTime;
            }
            Debug.DrawLine(GravityBody.Position, spawnVector);
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
