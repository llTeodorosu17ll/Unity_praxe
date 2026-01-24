// DoorInteract.cs
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class DoorInteract : MonoBehaviour
{
    [Header("Interact")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private int keysCost = 1;

    [Header("Navigation")]
    [SerializeField] private NavMeshObstacle navObstacle;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private string closeTriggerName = "Close";

    [Header("UI Hint")]
    [SerializeField] private UIHint uiHint;
    [SerializeField] private string msgOpenFree = "E - Open";
    [SerializeField] private string msgOpenUsesKey = "E - Open (uses 1 key)";
    [SerializeField] private string msgClose = "E - Close";
    [SerializeField] private string msgNeedKey = "Need a key";

    [Header("Initial State")]
    [SerializeField] private bool startUnlocked = false;
    [SerializeField] private bool startOpen = false;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private bool playerInside;
    private bool isOpen;
    private bool unlocked;

    // ---------- Save/Load API ----------
    public string DoorId => gameObject.name;
    public bool IsOpen => isOpen;
    public bool IsUnlocked => unlocked;

    // ---------- Unity event functions ----------
    private void Awake()
    {
        unlocked = startUnlocked;
        isOpen = startOpen;

        if (uiHint == null)
            uiHint = FindFirstObjectByType<UIHint>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null) animator = GetComponentInParent<Animator>();
        }

        if (navObstacle == null)
            navObstacle = GetComponent<NavMeshObstacle>();

        // Apply initial open/close to obstacle
        if (navObstacle != null)
            navObstacle.enabled = !isOpen;

        if (debugLogs) Debug.Log($"DoorInteract Awake: {name} (unlocked={unlocked}, open={isOpen})", this);
    }

    private void Update()
    {
        if (!playerInside) return;

        RefreshHint();

        if (!PressedInteract()) return;

        if (!isOpen)
        {
            // Try OPEN
            if (unlocked)
            {
                OpenDoor();
                return;
            }

            // Locked: need keys
            if (KeyManager.Instance != null && KeyManager.Instance.TrySpend(keysCost))
            {
                unlocked = true;
                OpenDoor();
                return;
            }

            // still locked
            RefreshHint();
        }
        else
        {
            // CLOSE
            CloseDoor();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = true;
        if (debugLogs) Debug.Log($"Door '{name}': player entered trigger", this);
        RefreshHint();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = false;
        if (debugLogs) Debug.Log($"Door '{name}': player left trigger", this);

        uiHint?.Hide();
    }

    // ---------- Public (Save/Load) ----------
    public void ApplySavedState(bool unlockedValue, bool openValue)
    {
        unlocked = unlockedValue;

        if (openValue)
            OpenDoor(playAnimation: true);
        else
            CloseDoor(playAnimation: true);

        if (debugLogs) Debug.Log($"Door '{name}' ApplySavedState: unlocked={unlocked}, open={isOpen}", this);
    }

    // ---------- Internal logic ----------
    private void OpenDoor(bool playAnimation = true)
    {
        isOpen = true;

        if (navObstacle != null)
            navObstacle.enabled = false;

        if (playAnimation && animator != null)
        {
            animator.ResetTrigger(closeTriggerName);
            animator.SetTrigger(openTriggerName);
        }

        RefreshHint();
    }

    private void CloseDoor(bool playAnimation = true)
    {
        isOpen = false;

        if (navObstacle != null)
            navObstacle.enabled = true;

        if (playAnimation && animator != null)
        {
            animator.ResetTrigger(openTriggerName);
            animator.SetTrigger(closeTriggerName);
        }

        RefreshHint();
    }

    private void RefreshHint()
    {
        if (uiHint == null) return;
        if (!playerInside) { uiHint.Hide(); return; }

        if (isOpen)
        {
            uiHint.Show(msgClose);
            return;
        }

        if (unlocked)
        {
            uiHint.Show(msgOpenFree);
            return;
        }

        bool hasKey = KeyManager.Instance != null && KeyManager.Instance.Keys >= keysCost;
        uiHint.Show(hasKey ? msgOpenUsesKey : msgNeedKey);
    }

    private bool PressedInteract()
    {
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    }
}
