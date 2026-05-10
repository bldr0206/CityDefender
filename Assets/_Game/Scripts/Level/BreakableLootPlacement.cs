using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public sealed class LootScatterAngleWave
{
    public readonly HashSet<int> Blocked = new HashSet<int>();
    public readonly HashSet<int> SucceededAngles = new HashSet<int>();
}

public static class BreakableLootPlacement
{
    const int AngleSlots = 36;
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
            if (!PickAngle(wave.Blocked, wave.SucceededAngles, out int angleIndex))
                angleIndex = Random.Range(0, AngleSlots);

            Vector3 candidate = CandidateAtAngle(originFlat, maxScatterDistance, angleIndex);

            if (ValidateLanding(originFlat, maxScatterDistance, candidate, navMeshSampleRadius,
                    lootSeparation, placedThisWave, out Vector3 landed))
            {
                wave.SucceededAngles.Add(angleIndex);
                return landed;
            }

            wave.Blocked.Add(angleIndex);
            if (wave.SucceededAngles.Contains(angleIndex))
                wave.SucceededAngles.Remove(angleIndex);
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(originFlat, out hit, navMeshSampleRadius, NavMesh.AllAreas))
            return hit.position;

        return originFlat;
    }

    static bool PickAngle(HashSet<int> blocked, HashSet<int> succeeded, out int angleIndex)
    {
        List<int> primary = BuildPrimaryPool(blocked, succeeded);
        if (primary.Count > 0)
        {
            angleIndex = primary[Random.Range(0, primary.Count)];
            return true;
        }

        if (succeeded.Count > 0)
        {
            var reuse = new List<int>(succeeded);
            angleIndex = reuse[Random.Range(0, reuse.Count)];
            return true;
        }

        angleIndex = 0;
        return false;
    }

    static List<int> BuildPrimaryPool(HashSet<int> blocked, HashSet<int> succeeded)
    {
        var pool = new List<int>(AngleSlots);
        for (int i = 0; i < AngleSlots; i++)
        {
            if (!blocked.Contains(i) && !succeeded.Contains(i))
                pool.Add(i);
        }

        return pool;
    }

    static Vector3 CandidateAtAngle(Vector3 originFlat, float maxScatterDistance, int angleIndexDegreesSlot)
    {
        float yaw = angleIndexDegreesSlot * 10f;
        Vector3 dir = Quaternion.AngleAxis(yaw, Vector3.up) * Vector3.forward;
        return originFlat + dir * maxScatterDistance;
    }

    static bool ValidateLanding(
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
        float dx = p.x - originFlat.x;
        float dz = p.z - originFlat.z;
        float horizDist = Mathf.Sqrt(dx * dx + dz * dz);
        if (horizDist > maxScatterDistance + navMeshSampleRadius)
            return false;

        if (lootSeparation > 0f && placedThisWave != null)
        {
            float sq = lootSeparation * lootSeparation;
            for (int i = 0; i < placedThisWave.Count; i++)
            {
                Vector3 q = placedThisWave[i];
                float ox = p.x - q.x;
                float oz = p.z - q.z;
                if (ox * ox + oz * oz < sq)
                    return false;
            }
        }

        landed = p;
        return true;
    }
}
