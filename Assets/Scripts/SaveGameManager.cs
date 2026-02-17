using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class EnemyState
{
    public string id;
    public Vector3 pos;
    public Quaternion rot;
    public bool chasingPlayer;
    public bool returning;
    public int waypointIndex;
}

[System.Serializable]
public class DoorState
{
    public string id;
    public bool unlocked;
    public bool open;
}

[System.Serializable]
public class SaveData
{
    public string sceneName;
    public Vector3 playerPos;
    public Quaternion playerRot;

    public float playerYaw;
    public float playerPitch;

    public int score;
    public int keys;

    public List<string> collectedPickups = new();
    public List<EnemyState> enemies = new();
    public List<DoorState> doors = new();
}

public class SaveGameManager : MonoBehaviour
{
    public static SaveGameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private KeyManager keyManager;

    private const string FileName = "save.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    private SaveData pending;
    private readonly HashSet<string> collectedPickupIds = new();
    private bool isLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ================= SAVE =================

    public void Save()
    {
        if (isLoading) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var movement = player.GetComponent<PlayerMovement>();

        scoreManager = FindFirstObjectByType<ScoreManager>();
        keyManager = FindFirstObjectByType<KeyManager>();

        SaveData data = new SaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            playerPos = player.transform.position,
            playerRot = player.transform.rotation,

            playerYaw = movement != null ? movement.GetYaw() : 0f,
            playerPitch = movement != null ? movement.GetPitch() : 0f,

            score = scoreManager != null ? scoreManager.Score : 0,
            keys = keyManager != null ? keyManager.Keys : 0,
            collectedPickups = new List<string>(collectedPickupIds)
        };

        foreach (var enemy in FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None))
        {
            data.enemies.Add(new EnemyState
            {
                id = enemy.gameObject.name,
                pos = enemy.transform.position,
                rot = enemy.transform.rotation,
                chasingPlayer = enemy.IsChasingPlayer,
                returning = enemy.IsReturning,
                waypointIndex = enemy.CurrentWaypointIndex
            });
        }

        foreach (var d in GameObject.FindGameObjectsWithTag("Door"))
        {
            var door = d.GetComponent<DoorInteract>();
            if (door == null) continue;

            data.doors.Add(new DoorState
            {
                id = door.DoorId,
                unlocked = door.IsUnlocked,
                open = door.IsOpen
            });
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        Debug.Log("Game saved.");
    }

    // ================= LOAD =================

    public void Load()
    {
        if (isLoading) return;

        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("No save file found.");
            return;
        }

        pending = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));

        collectedPickupIds.Clear();
        if (pending.collectedPickups != null)
        {
            foreach (var id in pending.collectedPickups)
                collectedPickupIds.Add(id);
        }

        isLoading = true;
        SceneManager.LoadScene(pending.sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isLoading || pending == null) return;
        StartCoroutine(ApplyAfterSpawn());
    }

    private IEnumerator ApplyAfterSpawn()
    {
        yield return null;
        yield return null;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            isLoading = false;
            yield break;
        }

        var movement = player.GetComponent<PlayerMovement>();
        var cc = player.GetComponent<CharacterController>();
        var agent = player.GetComponent<NavMeshAgent>();

        bool ccWas = cc != null && cc.enabled;
        bool agentWas = agent != null && agent.enabled;

        if (agent != null) agent.enabled = false;
        if (cc != null) cc.enabled = false;

        player.transform.SetPositionAndRotation(pending.playerPos, pending.playerRot);

        if (movement != null)
            movement.SetLookRotation(pending.playerYaw, pending.playerPitch);

        yield return null;

        if (cc != null) cc.enabled = ccWas;
        if (agent != null) agent.enabled = agentWas;

        scoreManager = FindFirstObjectByType<ScoreManager>();
        keyManager = FindFirstObjectByType<KeyManager>();

        if (scoreManager != null)
            scoreManager.SetScore(pending.score);

        if (keyManager != null)
            keyManager.SetKeys(pending.keys);

        RestoreEnemies();
        RestoreDoors();
        ApplyCollectedPickupsInScene();

        pending = null;
        isLoading = false;

        Debug.Log("Load complete.");
    }

    private void RestoreEnemies()
    {
        var map = new Dictionary<string, EnemyState>();
        foreach (var st in pending.enemies)
            map[st.id] = st;

        foreach (var enemy in FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None))
        {
            if (map.TryGetValue(enemy.gameObject.name, out var st))
            {
                enemy.transform.SetPositionAndRotation(st.pos, st.rot);
                enemy.ApplySavedAIState(st.chasingPlayer, st.returning, st.waypointIndex);
            }
        }
    }

    private void RestoreDoors()
    {
        var map = new Dictionary<string, DoorState>();
        foreach (var ds in pending.doors)
            map[ds.id] = ds;

        foreach (var d in GameObject.FindGameObjectsWithTag("Door"))
        {
            var door = d.GetComponent<DoorInteract>();
            if (door == null) continue;

            if (map.TryGetValue(door.DoorId, out var ds))
                door.ApplySavedState(ds.unlocked, ds.open);
        }
    }

    private void ApplyCollectedPickupsInScene()
    {
        var pickups = FindObjectsByType<PickUpScript>(FindObjectsSortMode.None);
        foreach (var p in pickups)
        {
            if (!string.IsNullOrEmpty(p.PickupId) &&
                collectedPickupIds.Contains(p.PickupId))
            {
                p.gameObject.SetActive(false);
            }
        }
    }

    public void MarkPickupCollected(string pickupId)
    {
        if (!string.IsNullOrEmpty(pickupId))
            collectedPickupIds.Add(pickupId);
    }

    public bool IsPickupCollected(string pickupId)
    {
        return collectedPickupIds.Contains(pickupId);
    }
}
