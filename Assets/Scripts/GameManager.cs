using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public sealed class GameManager : MonoBehaviour
{
    [Serializable]
    private class EnemySave
    {
        public string id;
        public Vector3 pos;
        public Quaternion rot;
        public bool chasingPlayer;
        public bool returning;
        public int waypointIndex;
    }

    [Serializable]
    private class DoorSave
    {
        public string id;
        public bool unlocked;
        public bool open;
    }

    [Serializable]
    private class SaveData
    {
        public int saveVersion = 3;

        public string sceneName;

        public Vector3 playerPos;
        public Quaternion playerRot;
        public float playerYaw;
        public float playerPitch;

        public int score;
        public int keys;

        public int spawnSeed;

        public float stamina;
        public float flashlightBattery;
        public bool flashlightOn;

        public List<string> collectedPickups = new();
        public List<EnemySave> enemies = new();
        public List<DoorSave> doors = new();
    }

    [Serializable]
    private struct SceneTransferData
    {
        public int score;
        public int keys;
        public float stamina;
        public float flashlightBattery;
        public bool flashlightOn;
    }

    public static GameManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    [Header("Runtime state")]
    [SerializeField] private int score;
    [SerializeField] private int keys;

    [Header("Registered refs")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private StaminaSystem staminaSystem;
    [SerializeField] private FlashlightSystem flashlightSystem;
    [SerializeField] private UIHint uiHint;
    [SerializeField] private SpawnManager spawnManager;

    public event Action<int> ScoreChanged;
    public event Action<int> KeysChanged;

    private readonly HashSet<string> collectedPickupIds = new();

    private const string FileName = "save.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    private SaveData pendingLoad;
    private bool isLoading;

    private bool hasPendingSceneTransfer;
    private SceneTransferData pendingSceneTransfer;

    public bool IsLoading => isLoading;

    public int Score => score;
    public int Keys => keys;

    public PlayerMovement PlayerMovement => playerMovement;
    public StaminaSystem StaminaSystem => staminaSystem;
    public FlashlightSystem FlashlightSystem => flashlightSystem;
    public UIHint UIHint => uiHint;
    public SpawnManager SpawnManager => spawnManager;
    public Transform PlayerTransform => playerMovement != null ? playerMovement.transform : null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject(nameof(GameManager));
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<GameManager>();
    }

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
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ----------------------------
    // Registration
    // ----------------------------
    public void RegisterPlayer(PlayerMovement movement, StaminaSystem stamina, FlashlightSystem flashlight)
    {
        playerMovement = movement;
        staminaSystem = stamina;
        flashlightSystem = flashlight;
    }

    public void UnregisterPlayer(PlayerMovement movement)
    {
        if (playerMovement != movement)
            return;

        playerMovement = null;
        staminaSystem = null;
        flashlightSystem = null;
    }

    public void RegisterUIHint(UIHint hint)
    {
        uiHint = hint;
    }

    public void UnregisterUIHint(UIHint hint)
    {
        if (uiHint == hint)
            uiHint = null;
    }

    public void RegisterSpawnManager(SpawnManager manager)
    {
        spawnManager = manager;
    }

    public void UnregisterSpawnManager(SpawnManager manager)
    {
        if (spawnManager == manager)
            spawnManager = null;
    }

    // ----------------------------
    // Score / Keys
    // ----------------------------
    public void AddScore(int amount)
    {
        if (amount == 0)
            return;

        SetScore(score + amount);
    }

    public void SetScore(int value)
    {
        int newValue = Mathf.Max(0, value);
        if (score == newValue)
            return;

        score = newValue;
        ScoreChanged?.Invoke(score);
    }

    public void ResetScore()
    {
        SetScore(0);
    }

    public void AddKeys(int amount)
    {
        if (amount == 0)
            return;

        SetKeys(keys + amount);
    }

    public void SetKeys(int value)
    {
        int newValue = Mathf.Max(0, value);
        if (keys == newValue)
            return;

        keys = newValue;
        KeysChanged?.Invoke(keys);
    }

    public void ResetKeys()
    {
        SetKeys(0);
    }

    public bool TrySpendKeys(int amount = 1)
    {
        int clampedAmount = Mathf.Max(0, amount);

        if (clampedAmount == 0)
            return true;

        if (keys < clampedAmount)
            return false;

        SetKeys(keys - clampedAmount);
        return true;
    }

    // ----------------------------
    // Pickups
    // ----------------------------
    public void MarkPickupCollected(string pickupId)
    {
        if (!string.IsNullOrWhiteSpace(pickupId))
            collectedPickupIds.Add(pickupId);
    }

    public bool IsPickupCollected(string pickupId)
    {
        return !string.IsNullOrWhiteSpace(pickupId) && collectedPickupIds.Contains(pickupId);
    }

    // ----------------------------
    // UI Button helpers
    // ----------------------------
    public void SaveGame_Button()
    {
        SaveGame();
    }

    public void LoadGame_Button()
    {
        LoadGame();
    }

    // ----------------------------
    // Save / Load
    // ----------------------------
    public void SaveGame()
    {
        if (isLoading)
            return;

        if (playerMovement == null)
        {
            Debug.LogWarning("GameManager: PlayerMovement is not registered, cannot save.");
            return;
        }

        SaveData data = new SaveData
        {
            saveVersion = 3,
            sceneName = SceneManager.GetActiveScene().name,

            playerPos = playerMovement.transform.position,
            playerRot = playerMovement.transform.rotation,
            playerYaw = playerMovement.GetYaw(),
            playerPitch = playerMovement.GetPitch(),

            score = score,
            keys = keys,

            spawnSeed = spawnManager != null ? spawnManager.CurrentSeed : 0,

            stamina = staminaSystem != null ? staminaSystem.CurrentStamina : 100f,
            flashlightBattery = flashlightSystem != null ? flashlightSystem.CurrentBattery : 100f,
            flashlightOn = flashlightSystem != null && flashlightSystem.IsOn,

            collectedPickups = new List<string>(collectedPickupIds)
        };

        EnemyMovement[] enemies = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyMovement enemy = enemies[i];
            if (enemy == null) continue;

            data.enemies.Add(new EnemySave
            {
                id = enemy.gameObject.name,
                pos = enemy.transform.position,
                rot = enemy.transform.rotation,
                chasingPlayer = enemy.IsChasingPlayer,
                returning = enemy.IsReturning,
                waypointIndex = enemy.CurrentWaypointIndex
            });
        }

        DoorInteract[] doors = FindObjectsByType<DoorInteract>(FindObjectsSortMode.None);
        for (int i = 0; i < doors.Length; i++)
        {
            DoorInteract door = doors[i];
            if (door == null) continue;

            data.doors.Add(new DoorSave
            {
                id = door.DoorId,
                unlocked = door.IsUnlocked,
                open = door.IsOpen
            });
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        Debug.Log("Game saved.");
    }

    public void LoadGame()
    {
        if (isLoading)
            return;

        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("No save file found.");
            return;
        }

        pendingLoad = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        if (pendingLoad == null)
        {
            Debug.LogError("Save file is invalid.");
            return;
        }

        collectedPickupIds.Clear();
        if (pendingLoad.collectedPickups != null)
        {
            for (int i = 0; i < pendingLoad.collectedPickups.Count; i++)
                collectedPickupIds.Add(pendingLoad.collectedPickups[i]);
        }

        isLoading = true;
        SceneManager.LoadScene(pendingLoad.sceneName);
    }

    // ----------------------------
    // Scene transfer
    // ----------------------------
    public void CaptureSceneTransferState()
    {
        pendingSceneTransfer = new SceneTransferData
        {
            score = score,
            keys = keys,
            stamina = staminaSystem != null ? staminaSystem.CurrentStamina : 0f,
            flashlightBattery = flashlightSystem != null ? flashlightSystem.CurrentBattery : 0f,
            flashlightOn = flashlightSystem != null && flashlightSystem.IsOn
        };

        hasPendingSceneTransfer = true;
    }

    public bool ContinueToNextLevel()
    {
        if (!hasPendingSceneTransfer)
            CaptureSceneTransferState();

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
            return false;

        SceneManager.LoadScene(nextIndex);
        return true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isLoading && pendingLoad != null)
        {
            StartCoroutine(ApplyLoadedGameAfterSceneInit());
            return;
        }

        if (hasPendingSceneTransfer)
            StartCoroutine(ApplySceneTransferAfterSceneInit());
    }

    private IEnumerator WaitForSceneRefs(int maxFrames = 12)
    {
        int frames = 0;

        while (frames < maxFrames)
        {
            bool playerReady = playerMovement != null;
            bool spawnReady = spawnManager != null || pendingLoad == null;

            if (playerReady && spawnReady)
                yield break;

            frames++;
            yield return null;
        }
    }

    private IEnumerator ApplyLoadedGameAfterSceneInit()
    {
        yield return null;
        yield return null;
        yield return StartCoroutine(WaitForSceneRefs());

        if (spawnManager != null)
            spawnManager.GenerateNewLayout(pendingLoad.spawnSeed);

        yield return null;
        yield return null;
        yield return StartCoroutine(WaitForSceneRefs());

        if (playerMovement == null)
        {
            Debug.LogError("GameManager: player was not registered after scene load.");
            pendingLoad = null;
            isLoading = false;
            yield break;
        }

        CharacterController cc = playerMovement.GetComponent<CharacterController>();
        NavMeshAgent agent = playerMovement.GetComponent<NavMeshAgent>();

        bool ccWasEnabled = cc != null && cc.enabled;
        bool agentWasEnabled = agent != null && agent.enabled;

        if (agent != null) agent.enabled = false;
        if (cc != null) cc.enabled = false;

        playerMovement.transform.SetPositionAndRotation(pendingLoad.playerPos, pendingLoad.playerRot);
        playerMovement.SetLookRotation(pendingLoad.playerYaw, pendingLoad.playerPitch);

        yield return null;

        if (cc != null) cc.enabled = ccWasEnabled;
        if (agent != null) agent.enabled = agentWasEnabled;

        SetScore(pendingLoad.score);
        SetKeys(pendingLoad.keys);

        if (staminaSystem != null)
            staminaSystem.SetCurrentStamina(pendingLoad.stamina);

        if (flashlightSystem != null)
            flashlightSystem.SetBatteryAndState(
                pendingLoad.flashlightBattery,
                pendingLoad.flashlightOn,
                true);

        RestoreEnemies();
        RestoreDoors();
        ApplyCollectedPickupsInScene();

        pendingLoad = null;
        isLoading = false;

        Debug.Log("Load complete.");
    }

    private IEnumerator ApplySceneTransferAfterSceneInit()
    {
        yield return null;
        yield return null;
        yield return StartCoroutine(WaitForSceneRefs());

        SetScore(pendingSceneTransfer.score);
        SetKeys(pendingSceneTransfer.keys);

        if (staminaSystem != null)
            staminaSystem.SetCurrentStamina(pendingSceneTransfer.stamina);

        if (flashlightSystem != null)
            flashlightSystem.SetBatteryAndState(
                pendingSceneTransfer.flashlightBattery,
                pendingSceneTransfer.flashlightOn,
                true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        hasPendingSceneTransfer = false;
    }

    private void RestoreEnemies()
    {
        if (pendingLoad == null || pendingLoad.enemies == null)
            return;

        Dictionary<string, EnemySave> map = new Dictionary<string, EnemySave>();
        for (int i = 0; i < pendingLoad.enemies.Count; i++)
        {
            EnemySave st = pendingLoad.enemies[i];
            if (st != null && !string.IsNullOrWhiteSpace(st.id))
                map[st.id] = st;
        }

        EnemyMovement[] enemies = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyMovement enemy = enemies[i];
            if (enemy == null) continue;

            if (map.TryGetValue(enemy.gameObject.name, out EnemySave st))
            {
                enemy.transform.SetPositionAndRotation(st.pos, st.rot);
                enemy.ApplySavedAIState(st.chasingPlayer, st.returning, st.waypointIndex);
            }
        }
    }

    private void RestoreDoors()
    {
        if (pendingLoad == null || pendingLoad.doors == null)
            return;

        Dictionary<string, DoorSave> map = new Dictionary<string, DoorSave>();
        for (int i = 0; i < pendingLoad.doors.Count; i++)
        {
            DoorSave ds = pendingLoad.doors[i];
            if (ds != null && !string.IsNullOrWhiteSpace(ds.id))
                map[ds.id] = ds;
        }

        DoorInteract[] doors = FindObjectsByType<DoorInteract>(FindObjectsSortMode.None);
        for (int i = 0; i < doors.Length; i++)
        {
            DoorInteract door = doors[i];
            if (door == null) continue;

            if (map.TryGetValue(door.DoorId, out DoorSave ds))
                door.ApplySavedState(ds.unlocked, ds.open);
        }
    }

    private void ApplyCollectedPickupsInScene()
    {
        PickUpScript[] pickups = FindObjectsByType<PickUpScript>(FindObjectsSortMode.None);
        for (int i = 0; i < pickups.Length; i++)
        {
            PickUpScript pickup = pickups[i];
            if (pickup == null) continue;

            if (!string.IsNullOrWhiteSpace(pickup.PickupId) && collectedPickupIds.Contains(pickup.PickupId))
                pickup.gameObject.SetActive(false);
        }
    }
}