using UnityEngine;

public sealed class TouchHazard : MonoBehaviour
{
    public void Hit(PlayerCheckpoint player)
    {
        if (player != null)
            player.Die();
    }
}
