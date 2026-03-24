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

    public string DoorId => gameObject.name;
    public bool IsOpen => isOpen;
    public bool IsUnlocked => unlocked;

    private void Awake()
    {
        unlocked = startUnlocked;
        isOpen = startOpen;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
            if (animator == null)
                animator = GetComponentInParent<Animator>();
        }

        if (navObstacle == null)
            navObstacle = GetComponent<NavMeshObstacle>();

        if (navObstacle != null)
            navObstacle.enabled = !isOpen;
    }

    private void Update()
    {
        if (!playerInside)
            return;

        if (uiHint == null && GameManager.HasInstance)
            uiHint = GameManager.Instance.UIHint;

        RefreshHint();

        if (!PressedInteract())
            return;

        if (!isOpen)
        {
            if (unlocked)
            {
                OpenDoor();
                return;
            }

            if (!GameManager.HasInstance)
                return;

            if (GameManager.Instance.TrySpendKeys(keysCost))
            {
                unlocked = true;
                OpenDoor();
                return;
            }

            RefreshHint();
        }
        else
        {
            CloseDoor();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || !other.CompareTag(playerTag))
            return;

        playerInside = true;
        RefreshHint();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null || !other.CompareTag(playerTag))
            return;

        playerInside = false;
        uiHint?.Hide();
    }

    public void ApplySavedState(bool unlockedValue, bool openValue)
    {
        unlocked = unlockedValue;

        if (openValue)
            OpenDoor(false);
        else
            CloseDoor(false);

        if (debugLogs)
            Debug.Log($"Door '{name}' ApplySavedState: unlocked={unlocked}, open={isOpen}", this);
    }

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
        if (uiHint == null)
            return;

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

        bool hasKey = GameManager.HasInstance && GameManager.Instance.Keys >= keysCost;
        uiHint.Show(hasKey ? msgOpenUsesKey : msgNeedKey);
    }

    private bool PressedInteract()
    {
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    }
}