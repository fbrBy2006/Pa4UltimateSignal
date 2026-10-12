using UnityEngine;

public sealed class LevelCompleteTrigger : MonoBehaviour
{
    [SerializeField] private CanvasGroup completionOverlay;
    [SerializeField] private CoinManager coinManager;
    [SerializeField] private RunRecordManager runRecordManager;
    [SerializeField, Min(1)] private int requiredCoins = 7;

    private bool completed;

    private void OnTriggerEnter(Collider other)
    {
        if (completed)
            return;

        FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
        if (player == null || completionOverlay == null)
            return;
        if (coinManager != null && coinManager.CollectedCount < requiredCoins)
            return;

        completed = true;
        if (runRecordManager != null)
            runRecordManager.CompleteRun();
        player.SetControlsEnabled(false);
        completionOverlay.alpha = 1f;
        completionOverlay.interactable = true;
        completionOverlay.blocksRaycasts = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
