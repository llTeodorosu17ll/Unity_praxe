using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PickUpScript : MonoBehaviour
{
    [Header("Identity (for Save/Load)")]
    [Tooltip("Unique ID of this pickup instance. Used to remember collected items after Load.")]
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
        if (string.IsNullOrEmpty(pickupId))
            pickupId = BuildAutoId();

        if (!string.IsNullOrEmpty(pickupId) && SaveGameManager.IsPickupCollected(pickupId))
            Destroy(gameObject);
    }

    private string BuildAutoId()
    {
        string scene = SceneManager.GetActiveScene().name;
        string path = GetPath(transform);
        Vector3 p = transform.position;
        return $"AUTO|{scene}|{path}|{p.x:F3},{p.y:F3},{p.z:F3}";
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(pickupId))
            pickupId = System.Guid.NewGuid().ToString("N");

        var all = FindObjectsByType<PickUpScript>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != this && all[i] != null && all[i].pickupId == pickupId)
            {
                pickupId = System.Guid.NewGuid().ToString("N");
                break;
            }
        }
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
