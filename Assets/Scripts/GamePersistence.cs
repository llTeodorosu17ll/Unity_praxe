using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[Serializable]
public class EnemyState
{
    public string id;
    public Vector3 pos;
    public Quaternion rot;
    public bool chasingPlayer;
    public bool returning;
    public int waypointIndex;
}

[Serializable]
public class DoorState
{
    public string id;
    public bool unlocked;
    public bool open;
}

[Serializable]
public class SaveData
{
    public int saveVersion = 4;

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
    public List<EnemyState> enemies = new();
    public List<DoorState> doors = new();
}

[RequireComponent(typeof(GameManager))]
[RequireComponent(typeof(GameWorldState))]
public class GamePersistence : MonoBehaviour
{
    [Serializable]
    private struct SceneTransferData
    {
        public int score;
        public int keys;
        public float stamina;
        public float flashlightBattery;
        public bool flashlightOn;
    }

    private const string FileName = "save.json";

    private GameManager gameManager;
    private GameWorldState worldState;

    private SaveData pendingLoad;
    private bool isLoading;

    private bool hasPendingSceneTransfer;
    private SceneTransferData pendingSceneTransfer;

    private string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public bool IsLoading => isLoading;

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
        worldState = GetComponent<GameWorldState>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded_Event;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded_Event;
    }

    public void SaveGame()
    {
        if (isLoading)
            return;

        if (gameManager.PlayerMovement == null)
        {
            Debug.LogWarning("GamePersistence: PlayerMovement is not registered, cannot save.", this);
            return;
        }

        SaveData data = new SaveData
        {
            saveVersion = 4,
            sceneName = SceneManager.GetActiveScene().name,

            playerPos = gameManager.PlayerMovement.transform.position,
            playerRot = gameManager.PlayerMovement.transform.rotation,
            playerYaw = gameManager.PlayerMovement.GetYaw(),
            playerPitch = gameManager.PlayerMovement.GetPitch(),

            score = gameManager.Score,
            keys = gameManager.Keys,

            spawnSeed = gameManager.SpawnManager != null ? gameManager.SpawnManager.CurrentSeed : 0,

            stamina = gameManager.StaminaSystem != null ? gameManager.StaminaSystem.CurrentStamina : 100f,
            flashlightBattery = gameManager.FlashlightSystem != null ? gameManager.FlashlightSystem.CurrentBattery : 100f,
            flashlightOn = gameManager.FlashlightSystem != null && gameManager.FlashlightSystem.IsOn
        };

        worldState.FillWorldState(data);

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        Debug.Log("Game saved.", this);
    }

    public void LoadGame()
    {
        if (isLoading)
            return;

        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("No save file found.", this);
            return;
        }

        pendingLoad = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        if (pendingLoad == null)
        {
            Debug.LogError("Save file is invalid.", this);
            return;
        }

        worldState.SetCollectedPickupIds(pendingLoad.collectedPickups);

        isLoading = true;
        SceneManager.LoadScene(pendingLoad.sceneName);
    }

    public void CaptureSceneTransferState()
    {
        pendingSceneTransfer = new SceneTransferData
        {
            score = gameManager.Score,
            keys = gameManager.Keys,
            stamina = gameManager.StaminaSystem != null ? gameManager.StaminaSystem.CurrentStamina : 0f,
            flashlightBattery = gameManager.FlashlightSystem != null ? gameManager.FlashlightSystem.CurrentBattery : 0f,
            flashlightOn = gameManager.FlashlightSystem != null && gameManager.FlashlightSystem.IsOn
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

        worldState.ClearCollectedPickups();

        SceneManager.LoadScene(nextIndex);
        return true;
    }

    private void OnSceneLoaded_Event(Scene scene, LoadSceneMode mode)
    {
        if (isLoading && pendingLoad != null)
        {
            StartCoroutine(ApplyLoadedGameAfterSceneInit());
            return;
        }

        if (hasPendingSceneTransfer)
            StartCoroutine(ApplySceneTransferAfterSceneInit());
    }

    private IEnumerator WaitForSceneRefs(int maxFrames = 16)
    {
        int frames = 0;

        while (frames < maxFrames)
        {
            bool playerReady = gameManager.PlayerMovement != null;
            bool spawnReady = gameManager.SpawnManager != null || pendingLoad == null;

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

        if (gameManager.SpawnManager != null)
            gameManager.SpawnManager.GenerateNewLayout(pendingLoad.spawnSeed);

        yield return null;
        yield return null;
        yield return StartCoroutine(WaitForSceneRefs());

        if (gameManager.PlayerMovement == null)
        {
            Debug.LogError("GamePersistence: player was not registered after scene load.", this);
            pendingLoad = null;
            isLoading = false;
            yield break;
        }

        CharacterController cc = gameManager.PlayerMovement.GetComponent<CharacterController>();
        NavMeshAgent agent = gameManager.PlayerMovement.GetComponent<NavMeshAgent>();

        bool ccWasEnabled = cc != null && cc.enabled;
        bool agentWasEnabled = agent != null && agent.enabled;

        if (agent != null) agent.enabled = false;
        if (cc != null) cc.enabled = false;

        gameManager.PlayerMovement.transform.SetPositionAndRotation(pendingLoad.playerPos, pendingLoad.playerRot);
        gameManager.PlayerMovement.SetLookRotation(pendingLoad.playerYaw, pendingLoad.playerPitch);

        yield return null;

        if (cc != null) cc.enabled = ccWasEnabled;
        if (agent != null) agent.enabled = agentWasEnabled;

        gameManager.SetScore(pendingLoad.score);
        gameManager.SetKeys(pendingLoad.keys);

        if (gameManager.StaminaSystem != null)
            gameManager.StaminaSystem.SetCurrentStamina(pendingLoad.stamina);

        if (gameManager.FlashlightSystem != null)
        {
            gameManager.FlashlightSystem.SetBatteryAndState(
                pendingLoad.flashlightBattery,
                pendingLoad.flashlightOn,
                true
            );
        }

        worldState.ApplyWorldState(pendingLoad);

        pendingLoad = null;
        isLoading = false;

        Debug.Log("Load complete.", this);
    }

    private IEnumerator ApplySceneTransferAfterSceneInit()
    {
        yield return null;
        yield return null;
        yield return StartCoroutine(WaitForSceneRefs());

        gameManager.SetScore(pendingSceneTransfer.score);
        gameManager.SetKeys(pendingSceneTransfer.keys);

        if (gameManager.StaminaSystem != null)
            gameManager.StaminaSystem.SetCurrentStamina(pendingSceneTransfer.stamina);

        if (gameManager.FlashlightSystem != null)
        {
            gameManager.FlashlightSystem.SetBatteryAndState(
                pendingSceneTransfer.flashlightBattery,
                pendingSceneTransfer.flashlightOn,
                true
            );
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        hasPendingSceneTransfer = false;
    }
}