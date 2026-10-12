using UnityEngine;

public sealed class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private Light indicatorLight;

    private PlayerCheckpoint playerCheckpoint;
    private bool activated;

    public void SetPlayerCheckpoint(PlayerCheckpoint player)
    {
        playerCheckpoint = player;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activated)
            return;

        PlayerCheckpoint player = other.GetComponentInParent<PlayerCheckpoint>();
        if (player == null)
            player = playerCheckpoint;
        if (player == null || respawnPoint == null)
            return;

        player.SetCheckpoint(respawnPoint);
        activated = true;
        if (indicatorLight != null)
        {
            indicatorLight.color = new Color(0.05f, 0.9f, 1f);
            indicatorLight.intensity = 2.4f;
        }
    }
}
