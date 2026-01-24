using UnityEngine;
using UnityEngine.AI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DoorInteract : MonoBehaviour
{
    [Header("Who can interact")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private NavMeshObstacle navObstacle;

    [Header("Keys")]
    [SerializeField] private int keysCost = 1;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private string closeTriggerName = "Close";

    [Header("Main Canvas Hint")]
    [SerializeField] private UIHint uiHint; // drag UIHint here OR it will auto-find
    [SerializeField] private string msgOpenFree = "E - Open";
    [SerializeField] private string msgOpenUsesKey = "E - Open (uses 1 key)";
    [SerializeField] private string msgClose = "E - Close";
    [SerializeField] private string msgNeedKey = "Need a key";

    [Header("State")]
    [SerializeField] private bool startUnlocked = false;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private bool playerInside;
    private bool isOpen;
    private bool unlocked;

    // ---------------- Unity event functions ----------------

    private void Awake()
    {
        if (debugLogs) Debug.Log("DoorInteract Awake: " + gameObject.name, this);

        unlocked = startUnlocked;

        if (uiHint == null)
            uiHint = FindFirstObjectByType<UIHint>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null) animator = GetComponentInParent<Animator>();
        }

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"DoorInteract: Animator/Controller missing on '{name}'. Assign Animator Controller.", this);
            enabled = false;
            return;
        }

        uiHint?.Hide();
    }

    private void Update()
    {
        if (!playerInside)
            return;

        RefreshHint();

        if (!PressedInteract())
            return;

        if (!isOpen)
        {
            // OPEN
            if (unlocked)
            {
                TriggerOpen();
                isOpen = true;
                RefreshHint();
                return;
            }

            // locked -> need key
            if (KeyManager.Instance == null)
            {
                RefreshHint();
                return;
            }

            if (KeyManager.Instance.TrySpend(keysCost))
            {
                unlocked = true; // once paid, stays unlocked forever
                TriggerOpen();
                isOpen = true;
            }

            RefreshHint();
        }
        else
        {
            // CLOSE (always allowed)
            TriggerClose();
            isOpen = false;
            RefreshHint();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = true;
        if (debugLogs) Debug.Log($"ENTER Door Trigger: {name} (playerInside = true)", this);

        RefreshHint();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = false;
        if (debugLogs) Debug.Log($"EXIT Door Trigger: {name} (playerInside = false)", this);

        uiHint?.Hide();
    }

    // ---------------- Internal logic ----------------

    private void TriggerOpen()
    {
        if (debugLogs) Debug.Log($"Door '{name}': OPEN", this);
        if (navObstacle != null) navObstacle.enabled = false;

        animator.ResetTrigger(closeTriggerName);
        animator.SetTrigger(openTriggerName);
    }

    private void TriggerClose()
    {
        if (debugLogs) Debug.Log($"Door '{name}': CLOSE", this);
        if (navObstacle != null) navObstacle.enabled = true;

        animator.ResetTrigger(openTriggerName);
        animator.SetTrigger(closeTriggerName);
    }

    private void RefreshHint()
    {
        if (debugLogs)
            Debug.Log("RefreshHint called. uiHint=" + (uiHint == null ? "NULL" : uiHint.name) + " playerInside=" + playerInside, this);

        if (uiHint == null) return;

        if (!playerInside)
        {
            uiHint.Hide();
            return;
        }

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
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }
}
