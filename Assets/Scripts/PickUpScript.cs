using UnityEngine;

[DisallowMultipleComponent]
public class PickUpScript : MonoBehaviour
{
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

    private void OnTriggerEnter(Collider other)
    {
        if (picked) return;
        if (!other.CompareTag(pickerTag)) return;

        picked = true;

        // logic effect
        if (effect != null)
            effect.Apply(other.gameObject);

        // audio (won't be cut off)
        if (pickupClip != null)
        {
            if (play3D)
                AudioSource.PlayClipAtPoint(pickupClip, transform.position, volume);
            else
                Play2DOneShot(pickupClip, volume);
        }

        // vfx
        if (pickupVfxPrefab != null)
        {
            var vfx = Instantiate(pickupVfxPrefab, transform.position, transform.rotation);
            Destroy(vfx, vfxLifetime);
        }

        // remove pickup
        if (destroyOnPickup) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    private static void Play2DOneShot(AudioClip clip, float volume)
    {
        var go = new GameObject("Pickup2DAudio");
        var src = go.AddComponent<AudioSource>();
        src.spatialBlend = 0f; // 2D
        src.PlayOneShot(clip, volume);
        Object.Destroy(go, clip.length + 0.1f);
    }
}
