using System.Collections.Generic;
using UnityEngine;

namespace CityDef.Gameplay.Logic
{
    /// <summary>
    /// Чистая геометрия прыжка со скалы: мировые точки пути для DOPath.
    /// Без NavMesh и Random — тестируется в EditMode.
    /// </summary>
    public static class CliffJumpArc
    {
        /// <summary>
        /// Абсолютные мировые точки пути: вершины «горбов» на хорде start→end
        /// + промежуточные впадины на хорде (при numJumps &gt; 1). Последняя точка — end.
        /// </summary>
        public static Vector3[] BuildWaypoints(Vector3 start, Vector3 end, float jumpPower, int numJumps)
        {
            numJumps = Mathf.Max(1, numJumps);
            var list = new List<Vector3>(numJumps * 2 + 2);
            for (int j = 0; j < numJumps; j++)
            {
                float t0 = j / (float)numJumps;
                float t1 = (j + 1) / (float)numJumps;
                float tPeak = (t0 + t1) * 0.5f;
                Vector3 peak = Vector3.Lerp(start, end, tPeak);
                peak.y += jumpPower;
                list.Add(peak);
                if (j < numJumps - 1)
                    list.Add(Vector3.Lerp(start, end, t1));
            }

            list.Add(end);
            return list.ToArray();
        }
    }
}
