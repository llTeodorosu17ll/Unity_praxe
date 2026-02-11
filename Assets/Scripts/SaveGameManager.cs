using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

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
        if (Instance != null && Instance != this)
            Destroy(Instance.gameObject);

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

        // stop input callbacks BEFORE scene unload destroys PlayerInput
        DeactivateAllPlayerInputs();

        Time.timeScale = 1f;

        pending = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));

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
        if (Instance != this) return;
        if (pending == null) return;
        StartCoroutine(ApplyAfterSpawn());
    }

    private IEnumerator ApplyAfterSpawn()
    {
        yield return null;

        GameObject player = null;
        float t = 0f;
        const float maxWait = 10f;

        while (player == null && t < maxWait)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (player == null)
        {
            Debug.LogError("SaveGameManager: Player not found after scene load. Check Player tag.");
            pending = null;
            yield break;
        }

        ApplyPlayerTransformSafely(player, pending.playerPos, pending.playerRot);
        yield return null;

        t = 0f;
        while ((ScoreManager.Instance == null || KeyManager.Instance == null) && t < maxWait)
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

        ApplyCollectedPickupsInScene();

        // re-enable input after everything is stable
        ReactivateAllPlayerInputs();

        pending = null;
    }

    private void ApplyPlayerTransformSafely(GameObject player, Vector3 pos, Quaternion rot)
    {
        var cc = player.GetComponent<CharacterController>();
        var agent = player.GetComponent<NavMeshAgent>();
        var rb = player.GetComponent<Rigidbody>();

        bool ccWasEnabled = cc != null && cc.enabled;
        bool agentWasEnabled = agent != null && agent.enabled;
        bool rbWasKinematic = rb != null && rb.isKinematic;

        if (agent != null) agent.enabled = false;
        if (cc != null) cc.enabled = false;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        player.transform.SetPositionAndRotation(pos, rot);

        if (rb != null) rb.isKinematic = rbWasKinematic;
        if (cc != null) cc.enabled = ccWasEnabled;
        if (agent != null) agent.enabled = agentWasEnabled;
    }

    private void ApplyCollectedPickupsInScene()
    {
        var pickups = FindObjectsByType<PickUpScript>(FindObjectsSortMode.None);
        for (int i = 0; i < pickups.Length; i++)
        {
            var p = pickups[i];
            if (p == null) continue;

            string id = p.PickupId;
            if (!string.IsNullOrEmpty(id) && collectedPickupIds.Contains(id))
                p.gameObject.SetActive(false);
        }
    }

    private static void DeactivateAllPlayerInputs()
    {
        var inputs = Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        for (int i = 0; i < inputs.Length; i++)
        {
            var pi = inputs[i];
            if (pi == null) continue;
            pi.DeactivateInput();
        }
    }

    private static void ReactivateAllPlayerInputs()
    {
        var inputs = Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        for (int i = 0; i < inputs.Length; i++)
        {
            var pi = inputs[i];
            if (pi == null) continue;
            if (!pi.isActiveAndEnabled) continue;
            pi.ActivateInput();
        }
    }
}
