using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerCheckpoint : MonoBehaviour
{
    [SerializeField, Min(1)] private int startingLives = 3;
    [SerializeField, Min(0f)] private float deathPause = 0.35f;
    [SerializeField, Min(0f)] private float respawnLockout = 0.2f;

    private CharacterController characterController;
    private FirstPersonController movement;
    [SerializeField] private TMP_Text livesCounter;
    [SerializeField] private CanvasGroup gameOverOverlay;
    private Vector3 checkpointPosition;
    private Quaternion checkpointRotation;
    private int livesRemaining;
    private bool isRespawning;
    private bool gameOver;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        movement = GetComponent<FirstPersonController>();
        checkpointPosition = transform.position;
        checkpointRotation = transform.rotation;
        livesRemaining = startingLives;
        RefreshLivesCounter();
    }

    private void Update()
    {
        if (gameOver && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SetCheckpoint(Transform point)
    {
        if (point == null)
            return;

        checkpointPosition = point.position;
        checkpointRotation = point.rotation;
    }

    public void Die()
    {
        if (isRespawning || gameOver)
            return;

        livesRemaining--;
        RefreshLivesCounter();
        if (livesRemaining <= 0)
        {
            gameOver = true;
            if (movement != null)
                movement.SetControlsEnabled(false);
            if (gameOverOverlay != null)
            {
                gameOverOverlay.alpha = 1f;
                gameOverOverlay.interactable = true;
                gameOverOverlay.blocksRaycasts = true;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        StartCoroutine(Respawn());
    }

    private void RefreshLivesCounter()
    {
        if (livesCounter != null)
            livesCounter.text = $"Vidas: {livesRemaining}/{startingLives}";
    }

    private IEnumerator Respawn()
    {
        isRespawning = true;
        if (movement != null)
            movement.SetControlsEnabled(false);

        yield return new WaitForSeconds(deathPause);

        characterController.enabled = false;
        transform.SetPositionAndRotation(checkpointPosition, checkpointRotation);
        if (movement != null)
            movement.ResetAfterRespawn();
        characterController.enabled = true;

        yield return new WaitForSeconds(respawnLockout);
        if (movement != null)
            movement.SetControlsEnabled(true);
        isRespawning = false;
    }
}
