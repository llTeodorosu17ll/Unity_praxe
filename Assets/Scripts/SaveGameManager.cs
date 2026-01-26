// SaveGameManager.cs
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public int score;
    public int keys;

    public List<string> collectedPickups = new();

    public List<EnemyState> enemies = new();
    public List<DoorState> doors = new();
}

public class SaveGameManager : MonoBehaviour
{
    public static SaveGameManager Instance { get; private set; }

    private const string FileName = "save.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    private static SaveData pending;

    // runtime registry of collected pickup IDs
    private static readonly HashSet<string> collectedPickupIds = new();

    public static void MarkPickupCollected(string pickupId)
    {
        if (string.IsNullOrEmpty(pickupId)) return;
        collectedPickupIds.Add(pickupId);
    }

    public static bool IsPickupCollected(string pickupId)
    {
        if (string.IsNullOrEmpty(pickupId)) return false;
        return collectedPickupIds.Contains(pickupId);
    }

    private void Awake()
    {
        // IMPORTANT: keep the newest scene instance (so UI references won't break)
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void Save()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("SaveGameManager: Player with tag 'Player' not found.");
            return;
        }

        SaveData data = new SaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            playerPos = player.transform.position,
            playerRot = player.transform.rotation,
            collectedPickups = new List<string>(collectedPickupIds)
        };

        if (ScoreManager.Instance != null) data.score = ScoreManager.Instance.Score;
        if (KeyManager.Instance != null) data.keys = KeyManager.Instance.Keys;

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

        GameObject[] doorObjects = GameObject.FindGameObjectsWithTag("Door");
        foreach (var d in doorObjects)
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
        Debug.Log("Saved to: " + SavePath);
    }

    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("SaveGameManager: No save file found at: " + SavePath);
            return;
        }

        Time.timeScale = 1f;

        pending = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));

        // make collected pickups available immediately (so pickups can delete themselves in Awake)
        collectedPickupIds.Clear();
        if (pending != null && pending.collectedPickups != null)
        {
            for (int i = 0; i < pending.collectedPickups.Count; i++)
                collectedPickupIds.Add(pending.collectedPickups[i]);
        }

        SceneManager.LoadScene(pending.sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // only the active instance should apply (older ones may still exist until end of frame)
        if (Instance != this) return;
        if (pending == null) return;

        StartCoroutine(ApplyAfterSpawn());
    }

    private IEnumerator ApplyAfterSpawn()
    {
        GameObject player = null;
        float t = 0f;
        while (player == null && t < 2f)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (player != null)
            player.transform.SetPositionAndRotation(pending.playerPos, pending.playerRot);

        t = 0f;
        while ((ScoreManager.Instance == null || KeyManager.Instance == null) && t < 2f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (ScoreManager.Instance != null) ScoreManager.Instance.SetScore(pending.score);
        if (KeyManager.Instance != null) KeyManager.Instance.SetKeys(pending.keys);

        var enemyMap = new Dictionary<string, EnemyState>();
        foreach (var st in pending.enemies) enemyMap[st.id] = st;

        foreach (var enemy in FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None))
        {
            if (enemyMap.TryGetValue(enemy.gameObject.name, out var st))
            {
                enemy.transform.SetPositionAndRotation(st.pos, st.rot);
                enemy.ApplySavedAIState(st.chasingPlayer, st.returning, st.waypointIndex);
            }
        }

        var doorMap = new Dictionary<string, DoorState>();
        foreach (var ds in pending.doors) doorMap[ds.id] = ds;

        GameObject[] doorObjects = GameObject.FindGameObjectsWithTag("Door");
        foreach (var d in doorObjects)
        {
            var door = d.GetComponent<DoorInteract>();
            if (door == null) continue;

            if (doorMap.TryGetValue(door.DoorId, out var ds))
                door.ApplySavedState(ds.unlocked, ds.open);
        }

        pending = null;
    }
}
