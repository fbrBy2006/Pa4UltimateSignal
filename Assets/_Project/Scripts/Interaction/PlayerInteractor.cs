using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField] private TMP_Text promptText;
    [SerializeField, Min(0.5f)] private float reach = 2.5f;

    private IInteractable currentTarget;

    private void Awake()
    {
        if (viewCamera == null)
            viewCamera = GetComponentInChildren<Camera>();
        SetPrompt(string.Empty);
    }

    private void Update()
    {
        currentTarget = FindTarget();
        SetPrompt(currentTarget != null ? currentTarget.Prompt : string.Empty);

        if (currentTarget != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            currentTarget.Interact();
    }

    private IInteractable FindTarget()
    {
        if (viewCamera == null || !Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward, out RaycastHit hit, reach))
            return null;

        return hit.collider.GetComponentInParent<IInteractable>();
    }

    private void SetPrompt(string value)
    {
        if (promptText != null)
            promptText.text = value;
    }
}
