// PickUpScript.cs
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PickUpScript : MonoBehaviour
{
    [Header("Identity (for Save/Load)")]
    [SerializeField] private string pickupId;

    [Header("Who can pick it up")]
    [SerializeField] private string pickerTag = "Player";

    [Header("Effects (optional)")]
    [SerializeField] private PickupEffect effect;
    [SerializeField] private bool destroyOnPickup = true;

    [Header("Audio (optional)")]
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool play3D = true;

    [Header("VFX (optional)")]
    [SerializeField] private GameObject pickupVfxPrefab;
    [SerializeField] private float vfxLifetime = 2f;

    private bool picked;

    public string PickupId => pickupId;

    private void Awake()
    {
        EnsureId();

        // If already collected in loaded save -> remove immediately
        if (!string.IsNullOrEmpty(pickupId) && SaveGameManager.IsPickupCollected(pickupId))
            Destroy(gameObject);
    }

    private void OnEnable()
    {
        // Safety: if object got enabled later (rare), remove it too
        if (!string.IsNullOrEmpty(pickupId) && SaveGameManager.IsPickupCollected(pickupId))
            Destroy(gameObject);
    }

    private void EnsureId()
    {
        // If you didn't bake IDs in editor, we generate a stable auto ID:
        // based on scene + hierarchy path with sibling indexes (stable across reloads).
        if (!string.IsNullOrEmpty(pickupId)) return;
        pickupId = BuildStableAutoId();
    }

    private string BuildStableAutoId()
    {
        string scene = SceneManager.GetActiveScene().name;
        return $"AUTO|{scene}|{GetPathWithIndices(transform)}";
    }

    private static string GetPathWithIndices(Transform t)
    {
        string part = $"{t.name}[{t.GetSiblingIndex()}]";
        while (t.parent != null)
        {
            t = t.parent;
            part = $"{t.name}[{t.GetSiblingIndex()}]/" + part;
        }
        return part;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Bake stable IDs into the scene (so saves work even after restarting Unity).
        if (string.IsNullOrEmpty(pickupId))
            pickupId = BuildStableAutoId();
    }
#endif

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

        if (destroyOnPickup) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    private static void Play2DOneShot(AudioClip clip, float volume)
    {
        var go = new GameObject("Pickup2DAudio");
        var src = go.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
        src.PlayOneShot(clip, volume);

        UnityEngine.Object.Destroy(go, clip.length + 0.1f);
    }
}
