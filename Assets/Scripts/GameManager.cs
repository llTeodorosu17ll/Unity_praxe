using System;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    [Header("State")]
    [SerializeField] private int score;
    [SerializeField] private int keys;

    [Header("Registered refs")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private StaminaSystem staminaSystem;
    [SerializeField] private FlashlightSystem flashlightSystem;
    [SerializeField] private UIHint uiHint;
    [SerializeField] private SpawnManager spawnManager;

    private GamePersistence persistence;
    private GameWorldState worldState;

    public event Action<int> ScoreChanged;
    public event Action<int> KeysChanged;

    public int Score => score;
    public int Keys => keys;

    public bool IsLoading => persistence != null && persistence.IsLoading;

    public PlayerMovement PlayerMovement => playerMovement;
    public StaminaSystem StaminaSystem => staminaSystem;
    public FlashlightSystem FlashlightSystem => flashlightSystem;
    public UIHint UIHint => uiHint;
    public SpawnManager SpawnManager => spawnManager;
    public Transform PlayerTransform => playerMovement != null ? playerMovement.transform : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        persistence = GetComponent<GamePersistence>();
        worldState = GetComponent<GameWorldState>();

        if (persistence == null)
            Debug.LogError("GameManager: GamePersistence component is missing.", this);

        if (worldState == null)
            Debug.LogError("GameManager: GameWorldState component is missing.", this);
    }

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

    public void RegisterEnemy(EnemyMovement enemy)
    {
        worldState?.RegisterEnemy(enemy);
    }

    public void UnregisterEnemy(EnemyMovement enemy)
    {
        worldState?.UnregisterEnemy(enemy);
    }

    public void RegisterDoor(DoorInteract door)
    {
        worldState?.RegisterDoor(door);
    }

    public void UnregisterDoor(DoorInteract door)
    {
        worldState?.UnregisterDoor(door);
    }

    public void RegisterPickup(PickUpScript pickup)
    {
        worldState?.RegisterPickup(pickup);
    }

    public void UnregisterPickup(PickUpScript pickup)
    {
        worldState?.UnregisterPickup(pickup);
    }

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

    public void MarkPickupCollected(string pickupId)
    {
        worldState?.MarkPickupCollected(pickupId);
    }

    public bool IsPickupCollected(string pickupId)
    {
        return worldState != null && worldState.IsPickupCollected(pickupId);
    }

    public void SaveGame()
    {
        persistence?.SaveGame();
    }

    public void LoadGame()
    {
        persistence?.LoadGame();
    }

    public void CaptureSceneTransferState()
    {
        persistence?.CaptureSceneTransferState();
    }

    public bool ContinueToNextLevel()
    {
        return persistence != null && persistence.ContinueToNextLevel();
    }
}