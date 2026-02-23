using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Header("Allowed Prefabs")]
    [SerializeField] private List<GameObject> allowedPrefabs = new();

    [Range(0f, 1f)]
    [SerializeField] private float spawnChance = 1f;

    [Tooltip("If true, this spawn point will spawn at most one object.")]
    [SerializeField] private bool spawnOnlyOne = true;

    public List<GameObject> AllowedPrefabs => allowedPrefabs;
    public float SpawnChance => spawnChance;
    public bool SpawnOnlyOne => spawnOnlyOne;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
#endif
}