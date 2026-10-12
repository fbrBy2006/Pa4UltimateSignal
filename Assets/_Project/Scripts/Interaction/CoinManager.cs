using TMPro;
using UnityEngine;

public sealed class CoinManager : MonoBehaviour
{
    [SerializeField, Min(1)] private int totalCoins = 7;
    [SerializeField] private TMP_Text counterText;

    public int CollectedCount { get; private set; }

    private void Start() => RefreshCounter();

    public void Collect()
    {
        if (CollectedCount >= totalCoins)
            return;

        CollectedCount++;
        RefreshCounter();
    }

    public void RefreshCounter()
    {
        if (counterText != null)
            counterText.text = $"Celdas: {CollectedCount}/{totalCoins}";
    }
}
