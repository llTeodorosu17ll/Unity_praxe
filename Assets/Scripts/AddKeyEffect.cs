using UnityEngine;

public class AddKeyEffect : MonoBehaviour
{
    [SerializeField] private int amount = 1;
    [SerializeField] private KeyManager keyManager;

    public void Apply()
    {
        if (keyManager == null)
        {
            Debug.LogError("AddKeyEffect: KeyManager reference not assigned.");
            return;
        }

        for (int i = 0; i < amount; i++)
            keyManager.AddKey();
    }
}
