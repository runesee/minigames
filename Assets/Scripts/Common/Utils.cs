using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

struct SetupData : INetworkSerializable
{
    public FixedString64Bytes nickname;
    public FixedString64Bytes color;
    public FixedString64Bytes Guid;
    public Team team;

    public SetupData(FixedString64Bytes nickname, FixedString64Bytes color, FixedString64Bytes guid, Team team)
    {
        this.nickname = nickname;
        this.color = color;
        this.Guid = guid;
        this.team = team;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref nickname);
        serializer.SerializeValue(ref color);
        serializer.SerializeValue(ref Guid);
        serializer.SerializeValue(ref team);
    }
}

public static class PlayerUtils
{
    /// <summary>Helper function for getting the closest player within range and field of view, if any.</summary>
    /// <param name="range">Limit for how far a player can tag.</param>
    /// <returns> A GameObject of the same type as 'Player', or null.</returns>
    public static Player FindClosestPlayerInRange<Player>(float range, GameObject gameObject, Transform transform) where Player : Component
    {
        Player closest = null;
        float shortest = Mathf.Infinity;
        bool isWithinBounds = false;
        GameObject targetObject = null;
        Player taggedPlayer = null;

        foreach (var player in GameObject.FindObjectsByType<Player>(FindObjectsSortMode.None))
        {
            if (player.gameObject == gameObject) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance < range && distance < shortest)
            {
                shortest = distance;
                closest = player;
                Vector3 targetVector = (player.transform.position - transform.position).normalized;

                // Within bounds if angle between position diff vector and tagged player's forward vector < 45 degrees
                Quaternion.FromToRotation(transform.forward, targetVector).ToAngleAxis(out float angle, out Vector3 axis);
                isWithinBounds = Mathf.Abs(angle) <= (distance > range / 2 ? 70f : 45f);
                taggedPlayer = player;
                targetObject = player.gameObject;
            }
        }
        // Need to check whether a GameObject is blocking the player's view (e.g. a Cube)
        if (taggedPlayer != null && Physics.Linecast(transform.position, taggedPlayer.transform.position, out RaycastHit hit))
        {
            if (hit.collider.gameObject != targetObject) isWithinBounds = false;
        }
        return isWithinBounds ? closest : null;
    }
}