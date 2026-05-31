using System.Collections.Generic;
using UnityEngine;

public static class PlayerRegistry
{
    private static readonly HashSet<PlayerHealth> _players = new();
    private static readonly Dictionary<PlayerHealth, int> _targetCounts = new();

    private const float TargetCountPenalty = 5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        _players.Clear();
        _targetCounts.Clear();
    }

    public static int PlayerCount => _players.Count;

    public static void Register(PlayerHealth player)
    {
        _players.Add(player);
    }

    public static void Unregister(PlayerHealth player)
    {
        _players.Remove(player);
        _targetCounts.Remove(player);
    }

    public static void AssignTarget(PlayerHealth player)
    {
        if (player == null) return;
        _targetCounts.TryGetValue(player, out int count);
        _targetCounts[player] = count + 1;
    }

    public static void ReleaseTarget(PlayerHealth player)
    {
        if (player == null) return;
        if (_targetCounts.TryGetValue(player, out int count))
        {
            if (count <= 1)
                _targetCounts.Remove(player);
            else
                _targetCounts[player] = count - 1;
        }
    }

    public static int GetTargetCount(PlayerHealth player)
    {
        if (player == null) return 0;
        _targetCounts.TryGetValue(player, out int count);
        return count;
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

    public static PlayerHealth GetBestTarget(Vector3 position)
    {
        PlayerHealth best = null;
        float bestScore = float.MaxValue;

        foreach (var player in _players)
        {
            if (player.IsDown) continue;

            float dist = Vector3.Distance(position, player.transform.position);
            _targetCounts.TryGetValue(player, out int count);
            float score = dist + count * TargetCountPenalty;

            if (score < bestScore)
            {
                bestScore = score;
                best = player;
            }
        }

        return best;
    }
}
