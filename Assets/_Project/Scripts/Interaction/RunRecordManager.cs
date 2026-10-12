using System;
using TMPro;
using UnityEngine;

public sealed class RunRecordManager : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text bestRecordText;
    [SerializeField] private TMP_Text completionResultText;

    private float elapsedTime;
    private bool finished;
    private bool runStarted;
    private string recordKey;

    private void Awake()
    {
        recordKey = "BlueBoxBestTime_" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        RefreshBestRecord();
        RefreshTimer();
    }

    private void Update()
    {
        if (finished || !runStarted)
            return;

        elapsedTime += Time.deltaTime;
        RefreshTimer();
    }

    public void CompleteRun()
    {
        if (finished)
            return;

        finished = true;
        bool hasRecord = PlayerPrefs.HasKey(recordKey);
        float previousBest = PlayerPrefs.GetFloat(recordKey, float.MaxValue);
        bool isNewRecord = !hasRecord || elapsedTime < previousBest;

        if (isNewRecord)
        {
            PlayerPrefs.SetFloat(recordKey, elapsedTime);
            PlayerPrefs.Save();
        }

        RefreshBestRecord();
        if (completionResultText != null)
        {
            string result = $"Tiempo: {FormatTime(elapsedTime)}";
            completionResultText.text = isNewRecord
                ? result + "\n¡NUEVO RÉCORD!"
                : result + $"\nRécord: {FormatTime(previousBest)}";
        }
    }

    public void BeginRun()
    {
        if (!finished)
            runStarted = true;
    }

    private void RefreshTimer()
    {
        if (timerText != null)
            timerText.text = "Tiempo: " + FormatTime(elapsedTime);
    }

    private void RefreshBestRecord()
    {
        if (bestRecordText == null)
            return;

        bestRecordText.text = PlayerPrefs.HasKey(recordKey)
            ? "Récord: " + FormatTime(PlayerPrefs.GetFloat(recordKey))
            : "Récord: --:--.--";
    }

    private static string FormatTime(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);
        return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}.{time.Milliseconds / 10:00}";
    }
}
