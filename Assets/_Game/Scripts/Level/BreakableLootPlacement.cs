using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CityDef.Gameplay.Logic;

public sealed class LootScatterAngleWave
{
    public readonly HashSet<int> Blocked = new HashSet<int>();
    public readonly HashSet<int> SucceededAngles = new HashSet<int>();
}

public static class BreakableLootPlacement
{
    const int MaxAttempts = 36;

    public static Vector3 FindLandingPosition(
        Vector3 originFlat,
        float maxScatterDistance,
        float navMeshSampleRadius,
        float lootSeparation,
        LootScatterAngleWave wave,
        IList<Vector3> placedThisWave)
    {
        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            int angleIndex = PickAngle(wave.Blocked, wave.SucceededAngles);
            Vector3 candidate = LootScatter.CandidateAtAngle(
                originFlat, maxScatterDistance, angleIndex, LootScatter.AngleSlots);

            if (TrySampleLanding(originFlat, maxScatterDistance, candidate, navMeshSampleRadius,
                    lootSeparation, placedThisWave, out Vector3 landed))
            {
                wave.SucceededAngles.Add(angleIndex);
                return landed;
            }

            wave.Blocked.Add(angleIndex);
            wave.SucceededAngles.Remove(angleIndex);
        }

        if (NavMesh.SamplePosition(originFlat, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            return hit.position;

        return originFlat;
    }

    static int PickAngle(HashSet<int> blocked, HashSet<int> succeeded)
    {
        List<int> primary = LootScatter.BuildAnglePool(blocked, succeeded, LootScatter.AngleSlots);
        if (primary.Count > 0)
            return primary[Random.Range(0, primary.Count)];

        if (succeeded.Count > 0)
        {
            var reuse = new List<int>(succeeded);
            return reuse[Random.Range(0, reuse.Count)];
        }

        return Random.Range(0, LootScatter.AngleSlots);
    }

    static bool TrySampleLanding(
        Vector3 originFlat,
        float maxScatterDistance,
        Vector3 candidateHorizontal,
        float navMeshSampleRadius,
        float lootSeparation,
        IList<Vector3> placedThisWave,
        out Vector3 landed)
    {
        landed = default;
        if (!NavMesh.SamplePosition(candidateHorizontal, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            return false;

        Vector3 p = hit.position;
        if (!LootScatter.IsWithinScatter(originFlat, p, maxScatterDistance, navMeshSampleRadius))
            return false;

        if (!LootScatter.IsFarFromPlaced(p, placedThisWave, lootSeparation))
            return false;

        landed = p;
        return true;
    }
}
