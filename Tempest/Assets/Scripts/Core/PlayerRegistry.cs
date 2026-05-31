using System.Collections.Generic;
using UnityEngine;

public static class PlayerRegistry
{
    private static readonly HashSet<PlayerHealth> _players = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => _players.Clear();

    public static void Register(PlayerHealth player)
    {
        _players.Add(player);
    }

    public static void Unregister(PlayerHealth player)
    {
        _players.Remove(player);
    }

    public static PlayerHealth GetNearestPlayer(Vector3 position)
    {
        PlayerHealth nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var player in _players)
        {
            if (player.IsDown) continue;

            float dist = Vector3.Distance(position, player.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = player;
            }
        }

        return nearest;
    }
}
