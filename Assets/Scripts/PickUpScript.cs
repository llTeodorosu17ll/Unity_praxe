using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PickUpScript : MonoBehaviour
{
    [Header("Identity (for Save/Load)")]
    [SerializeField] private string pickupId;

    [Header("Who can pick it up")]
    [SerializeField] private string pickerTag = "Player";

    [Header("Effects (optional)")]
    [SerializeField] private PickupEffect effect;
    [SerializeField] private bool disableOnPickup = true;

    [Header("Audio (optional)")]
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool play3D = true;

    [Header("VFX (optional)")]
    [SerializeField] private GameObject pickupVfxPrefab;
    [SerializeField] private float vfxLifetime = 2f;

    private bool picked;

    public string PickupId => pickupId;

    // =========================
    // Unity native methods (order)
    // =========================
    private void Awake()
    {
        EnsureIdRuntime();

        // If already collected in loaded save -> remove immediately
        if (!string.IsNullOrEmpty(pickupId) && SaveGameManager.IsPickupCollected(pickupId))
            gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // Safety: if object got enabled later, remove it too
        if (!string.IsNullOrEmpty(pickupId) && SaveGameManager.IsPickupCollected(pickupId))
            gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (picked) return;
        if (!other.CompareTag(pickerTag)) return;

        picked = true;

        if (effect != null)
            effect.Apply(other.gameObject);

        if (!string.IsNullOrEmpty(pickupId))
            SaveGameManager.MarkPickupCollected(pickupId);

        if (pickupClip != null)
        {
            if (play3D)
                AudioSource.PlayClipAtPoint(pickupClip, transform.position, volume);
            else
                Play2DOneShot(pickupClip, volume);
        }

        if (pickupVfxPrefab != null)
        {
            var vfx = Instantiate(pickupVfxPrefab, transform.position, transform.rotation);
            Destroy(vfx, vfxLifetime);
        }

        if (disableOnPickup) gameObject.SetActive(false);
        else Destroy(gameObject);
    }

    // =========================
    // Internal helpers
    // =========================
    private void EnsureIdRuntime()
    {
        // In builds, we must never generate IDs based on hierarchy/path.
        // We only generate GUID if missing.
        if (string.IsNullOrEmpty(pickupId))
            pickupId = NewGuid();
    }

    private static string NewGuid()
    {
        // Compact but safe
        return Guid.NewGuid().ToString("N");
    }

    private static void Play2DOneShot(AudioClip clip, float volume)
    {
        var go = new GameObject("Pickup2DAudio");
        var src = go.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
        src.PlayOneShot(clip, volume);
        Destroy(go, clip.length + 0.1f);
    }

#if UNITY_EDITOR
    // Runs in Editor when you duplicate objects too.
    private void OnValidate()
    {
        // Ensure we have an ID
        if (string.IsNullOrEmpty(pickupId))
        {
            pickupId = NewGuid();
            return;
        }

        // IMPORTANT: Unity duplicates serialized fields when you duplicate GameObjects.
        // So we must detect duplicates and regenerate.
        var all = FindObjectsByType<PickUpScript>(FindObjectsSortMode.None);

        int sameCount = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            if (all[i].pickupId == pickupId) sameCount++;
            if (sameCount > 1) break;
        }

        if (sameCount > 1)
        {
            pickupId = NewGuid();
        }
    }
#endif
}
