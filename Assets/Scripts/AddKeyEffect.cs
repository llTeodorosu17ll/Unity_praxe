using UnityEngine;

[CreateAssetMenu(menuName = "Pickups/Effects/Add Key")]
public class AddKeyEffect : PickupEffect
{
    [SerializeField] private int amount = 1;

    public override void Apply(GameObject picker)
    {
        if (KeyManager.Instance != null)
            KeyManager.Instance.Add(amount);
    }
}
