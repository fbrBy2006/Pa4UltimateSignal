using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

public sealed class IntroSequenceController : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private CinemachineCamera introCamera;
    [SerializeField] private RunRecordManager runRecordManager;

    private TMP_Text instructionText;
    private bool gameplayStarted;

    private void Start()
    {
        SecurityDroneAI.EnsureSceneSystems();

        instructionText =
            GameObject.Find("MissionObjective")?.GetComponent<TMP_Text>();

        if (instructionText != null)
            instructionText.color = Color.black;

        if (director == null || director.playableAsset == null)
        {
            BeginGameplay();
            return;
        }

        if (playerController != null)
            playerController.SetControlsEnabled(false);

        director.stopped += OnDirectorStopped;
        director.Play();
    }

    private void OnDirectorStopped(PlayableDirector stoppedDirector)
    {
        BeginGameplay();
    }

    private void BeginGameplay()
    {
        if (gameplayStarted)
            return;

        gameplayStarted = true;

        if (introCamera != null)
            introCamera.gameObject.SetActive(false);

        if (playerController != null)
            playerController.SetControlsEnabled(true);

        if (runRecordManager != null)
            runRecordManager.BeginRun();

        if (instructionText != null)
        {
            instructionText.color = Color.black;
            instructionText.gameObject.SetActive(true);
            StartCoroutine(HideInstructionsAfterSevenSeconds());
        }
    }

    private IEnumerator HideInstructionsAfterSevenSeconds()
    {
        yield return new WaitForSecondsRealtime(7f);

        if (instructionText != null)
            instructionText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (director != null)
            director.stopped -= OnDirectorStopped;
    }
}