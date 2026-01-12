using UnityEngine;

[CreateAssetMenu(menuName = "Pickups/Effects/Add Score")]
public class ScoreAddEffect : PickupEffect
{
    [SerializeField] private int amount = 1;

    public override void Apply(GameObject picker)
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.Add(amount);
    }
}
