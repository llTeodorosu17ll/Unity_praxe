using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    private List<SpawnPoint> spawnPoints = new();
    private int currentSeed;

    public int CurrentSeed => currentSeed;

    private void Awake()
    {
        spawnPoints.AddRange(FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None));
    }

    public void GenerateNewLayout(int seed)
    {
        currentSeed = seed;
        Random.InitState(seed);

        foreach (var point in spawnPoints)
        {
            TrySpawnAtPoint(point);
        }
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

            Instantiate(prefab, point.transform.position, Quaternion.identity);
        }
        else
        {
            foreach (var prefab in point.AllowedPrefabs)
            {
                if (Random.value <= point.SpawnChance)
                {
                    Instantiate(prefab, point.transform.position, Quaternion.identity);
                }
            }
        }
    }
}