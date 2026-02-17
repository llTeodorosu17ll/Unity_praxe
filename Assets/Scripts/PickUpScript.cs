using UnityEngine;

public class PickUpScript : MonoBehaviour
{
    [SerializeField] private string pickupId;
    [SerializeField] private string playerTag = "Player";

    private SaveGameManager saveManager;
    private MonoBehaviour[] effects;
    private AudioSource audioSource;
    private bool collected;

    public string PickupId => pickupId;

    private void Awake()
    {
        saveManager = FindFirstObjectByType<SaveGameManager>();
        audioSource = GetComponent<AudioSource>();
        effects = GetComponents<MonoBehaviour>();

        if (string.IsNullOrEmpty(pickupId))
        {
            pickupId = gameObject.scene.name + "_" + gameObject.name + "_" + transform.position.ToString();
        }
    }

    private void Start()
    {
        if (saveManager != null && saveManager.IsPickupCollected(pickupId))
            gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag(playerTag)) return;

        Collect();
    }

    private void Collect()
    {
        collected = true;

        // Apply effects
        foreach (var effect in effects)
        {
            if (effect is ScoreAddEffect scoreEffect)
                scoreEffect.Apply();

            if (effect is AddKeyEffect keyEffect)
                keyEffect.Apply();
        }

        if (saveManager != null)
            saveManager.MarkPickupCollected(pickupId);

        PlaySoundDetached();

        gameObject.SetActive(false);
    }

    private void PlaySoundDetached()
    {
        if (audioSource == null || audioSource.clip == null)
            return;

        // Create temporary audio object
        GameObject temp = new GameObject("PickupSound");
        temp.transform.position = transform.position;

        AudioSource tempSource = temp.AddComponent<AudioSource>();
        tempSource.clip = audioSource.clip;
        tempSource.volume = audioSource.volume;
        tempSource.spatialBlend = audioSource.spatialBlend;

        tempSource.Play();

        Destroy(temp, tempSource.clip.length);
    }
}
