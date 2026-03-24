using System;
using System.Collections.Generic;
using UnityEngine;

public class PickUpScript : MonoBehaviour
{
    public enum PickupRewardType
    {
        Score,
        Key,
        Battery
    }

    [Serializable]
    public struct PickupReward
    {
        public PickupRewardType type;
        public float amount;
    }

    [Header("Identity")]
    [SerializeField] private string pickupId;

    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";

    [Header("Rewards")]
    [SerializeField] private List<PickupReward> rewards = new();

    private AudioSource audioSource;
    private bool collected;

    public string PickupId => pickupId;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (string.IsNullOrWhiteSpace(pickupId))
            pickupId = gameObject.scene.name + "_" + gameObject.name + "_" + transform.position;
    }

    private void Start()
    {
        if (GameManager.HasInstance && GameManager.Instance.IsPickupCollected(pickupId))
            gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        if (other == null || !other.CompareTag(playerTag))
            return;

        Collect();
    }

    private void Collect()
    {
        collected = true;

        ApplyRewards();

        if (GameManager.HasInstance)
            GameManager.Instance.MarkPickupCollected(pickupId);

        PlaySoundDetached();
        gameObject.SetActive(false);
    }

    private void ApplyRewards()
    {
        if (!GameManager.HasInstance)
            return;

        for (int i = 0; i < rewards.Count; i++)
        {
            PickupReward reward = rewards[i];

            switch (reward.type)
            {
                case PickupRewardType.Score:
                    GameManager.Instance.AddScore(Mathf.RoundToInt(reward.amount));
                    break;

                case PickupRewardType.Key:
                    GameManager.Instance.AddKeys(Mathf.RoundToInt(reward.amount));
                    break;

                case PickupRewardType.Battery:
                    if (GameManager.Instance.FlashlightSystem != null)
                        GameManager.Instance.FlashlightSystem.AddBattery(reward.amount);
                    break;
            }
        }
    }

    private void PlaySoundDetached()
    {
        if (audioSource == null || audioSource.clip == null)
            return;

        GameObject temp = new GameObject("PickupSound");
        temp.transform.position = transform.position;

        AudioSource tempSource = temp.AddComponent<AudioSource>();
        tempSource.clip = audioSource.clip;
        tempSource.volume = audioSource.volume;
        tempSource.spatialBlend = audioSource.spatialBlend;
        tempSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;

        tempSource.Play();
        Destroy(temp, tempSource.clip.length);
    }
}