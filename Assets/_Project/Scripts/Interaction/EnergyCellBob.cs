using UnityEngine;

public sealed class EnergyCellBob : MonoBehaviour
{
    [SerializeField, Min(0f)] private float bobHeight = 0.02f;
    [SerializeField, Min(0.1f)] private float bobSpeed = 2f;
    private Vector3 startPosition;

    private void Start() => startPosition = transform.localPosition;

    private void Update()
    {
        transform.localPosition = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        transform.Rotate(Vector3.up, 70f * Time.deltaTime, Space.Self);
    }
}
