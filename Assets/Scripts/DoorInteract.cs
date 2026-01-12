using UnityEngine;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DoorInteract : MonoBehaviour
{
    [Header("Who can interact")]
    [SerializeField] private string playerTag = "Player";

    [Header("Keys")]
    [SerializeField] private int keysCost = 1;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private string closeTriggerName = "Close";

    [Header("Hint UI (optional)")]
    [SerializeField] private TMP_Text hintText; // World Space TMP text near this door
    [SerializeField] private string msgOpenFree = "E - Open";
    [SerializeField] private string msgOpenUsesKey = "E - Open (uses 1 key)";
    [SerializeField] private string msgClose = "E - Close";
    [SerializeField] private string msgNeedKey = "Need a key";

    [Header("State")]
    [SerializeField] private bool startUnlocked = false;

    private bool playerInside;
    private bool isOpen;
    private bool unlocked;

    private void Awake()
    {
        unlocked = startUnlocked;

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

        // Make sure hint starts OFF
        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Hard guarantee: if not near door -> hint OFF and do nothing
        if (!playerInside)
        {
            if (hintText != null) hintText.gameObject.SetActive(false);
            return;
        }

        RefreshHint();

        if (!PressedInteract()) return;

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

            // not unlocked yet -> need a key
            if (KeyManager.Instance == null) { RefreshHint(); return; }

            if (KeyManager.Instance.TrySpend(keysCost))
            {
                unlocked = true; // once paid, door stays unlocked forever
                TriggerOpen();
                isOpen = true;
            }
            // else: no keys -> do nothing

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

    private void TriggerOpen()
    {
        animator.ResetTrigger(closeTriggerName);
        animator.SetTrigger(openTriggerName);
    }

    private void TriggerClose()
    {
        animator.ResetTrigger(openTriggerName);
        animator.SetTrigger(closeTriggerName);
    }

    private void RefreshHint()
    {
        if (hintText == null) return;

        // Only show while inside trigger
        hintText.gameObject.SetActive(playerInside);

        if (!playerInside) return;

        if (isOpen)
        {
            hintText.text = msgClose;
            return;
        }

        if (unlocked)
        {
            hintText.text = msgOpenFree;
            return;
        }

        bool hasKey = KeyManager.Instance != null && KeyManager.Instance.Keys >= keysCost;
        hintText.text = hasKey ? msgOpenUsesKey : msgNeedKey;
    }

    private bool PressedInteract()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = true;
        RefreshHint();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = false;

        // Immediately hide hint when leaving
        if (hintText != null) hintText.gameObject.SetActive(false);
    }
}
