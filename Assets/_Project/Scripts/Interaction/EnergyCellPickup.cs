using UnityEngine;

public sealed class EnergyCellPickup : MonoBehaviour
{
    [SerializeField] private CoinManager coinManager;
    private bool collected;
    private FirstPersonController playerInRange;

    private void OnTriggerEnter(Collider other)
    {
        CachePlayer(other);
        TryCollect();
    }

    private void OnTriggerStay(Collider other)
    {
        CachePlayer(other);
        TryCollect();
    }

    private void OnTriggerExit(Collider other)
    {
        FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
        if (player == playerInRange)
            playerInRange = null;
    }

    private void CachePlayer(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = other.GetComponentInParent<FirstPersonController>();
    }

    private void TryCollect()
    {
        if (collected || coinManager == null || playerInRange == null || !playerInRange.IsJumping)
            return;

        collected = true;
        coinManager.Collect();
        Destroy(gameObject);
    }
}
