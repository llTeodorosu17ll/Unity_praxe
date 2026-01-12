using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private float volume = 1f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        ScoreManager.Instance.Add(value);

        if (pickupClip != null)
            AudioSource.PlayClipAtPoint(pickupClip, transform.position, volume);

        Destroy(gameObject);
    }
}
