using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Auto Generate (New Game)")]
    [SerializeField] private bool generateOnStart = true;

    [Tooltip("If true: generate random seed on Start (New Game). If false: use fixed seed.")]
    [SerializeField] private bool useRandomSeed = true;

    [SerializeField] private int fixedSeed = 12345;

    private readonly List<SpawnPoint> spawnPoints = new();
    private readonly List<GameObject> spawnedObjects = new();

    private int currentSeed;
    public int CurrentSeed => currentSeed;

    private void Awake()
    {
        spawnPoints.Clear();
        spawnPoints.AddRange(FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None));

        // Deterministic order (important!)
        spawnPoints.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            int nameCmp = string.CompareOrdinal(a.gameObject.name, b.gameObject.name);
            if (nameCmp != 0) return nameCmp;

            // if same name, sort by position
            int x = a.transform.position.x.CompareTo(b.transform.position.x);
            if (x != 0) return x;
            int y = a.transform.position.y.CompareTo(b.transform.position.y);
            if (y != 0) return y;
            return a.transform.position.z.CompareTo(b.transform.position.z);
        });
    }

    private void Start()
    {
        if (!generateOnStart)
            return;

        // If we are loading a save, DO NOT generate random new items here.
        if (SaveGameManager.Instance != null && SaveGameManager.Instance.IsLoading)
            return;

        int seed = useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : fixedSeed;
        GenerateNewLayout(seed);
    }

    public void GenerateNewLayout(int seed)
    {
        currentSeed = seed;

        ClearSpawnedObjects();

        Random.State oldState = Random.state;
        Random.InitState(seed);

        foreach (var point in spawnPoints)
        {
            if (point == null) continue;
            TrySpawnAtPoint(point);
        }

        Random.state = oldState;
    }

    private void TrySpawnAtPoint(SpawnPoint point)
    {
        if (point.AllowedPrefabs == null || point.AllowedPrefabs.Count == 0)
            return;

        if (Random.value > point.SpawnChance)
            return;

        if (point.SpawnOnlyOne)
        {
            int index = Random.Range(0, point.AllowedPrefabs.Count);
            GameObject prefab = point.AllowedPrefabs[index];
            if (prefab == null) return;

            var spawned = Instantiate(prefab, point.transform.position, point.transform.rotation);
            spawnedObjects.Add(spawned);
        }
        else
        {
            foreach (var prefab in point.AllowedPrefabs)
            {
                if (prefab == null) continue;

                if (Random.value <= point.SpawnChance)
                {
                    var spawned = Instantiate(prefab, point.transform.position, point.transform.rotation);
                    spawnedObjects.Add(spawned);
                }
            }
        }
    }

    public void ClearSpawnedObjects()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
                Destroy(spawnedObjects[i]);
        }
        spawnedObjects.Clear();
    }
}