using System.Collections;
using UnityEngine;

public sealed class ReactorCore : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform sealedDoor;
    [SerializeField] private CoinManager coinManager;
    [SerializeField, Min(1)] private int requiredCoins = 7;
    [SerializeField] private Light[] lightsToActivate;
    [SerializeField] private ParticleSystem activationEffect;
    [SerializeField] private float doorOpenAngle = 90f;
    [SerializeField, Min(0.1f)] private float doorOpenDuration = 2f;

    private bool activated;
    private Quaternion doorClosedRotation;

    public string Prompt
    {
        get
        {
            if (activated) return string.Empty;
            if (coinManager == null || coinManager.CollectedCount < requiredCoins)
            {
                int collected = coinManager != null ? coinManager.CollectedCount : 0;
                return $"Núcleo bloqueado  |  Celdas: {collected}/{requiredCoins}";
            }
            return "E  Activar núcleo";
        }
    }

    private void Awake()
    {
        if (sealedDoor != null)
            doorClosedRotation = sealedDoor.localRotation;
    }

    public void Interact()
    {
        if (activated || coinManager == null || coinManager.CollectedCount < requiredCoins)
            return;

        activated = true;
        foreach (Light targetLight in lightsToActivate)
        {
            if (targetLight != null)
                targetLight.enabled = true;
        }

        if (activationEffect != null)
            activationEffect.Play();

        if (sealedDoor != null)
            StartCoroutine(OpenDoor());
    }

    private IEnumerator OpenDoor()
    {
        Quaternion openRotation = doorClosedRotation * Quaternion.Euler(0f, doorOpenAngle, 0f);
        float elapsed = 0f;

        while (elapsed < doorOpenDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / doorOpenDuration);
            sealedDoor.localRotation = Quaternion.Slerp(doorClosedRotation, openRotation, progress);
            yield return null;
        }

        sealedDoor.localRotation = openRotation;
    }
}
