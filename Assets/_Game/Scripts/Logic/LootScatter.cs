using System.Collections.Generic;
using UnityEngine;

namespace CityDef.Gameplay.Logic
{
    /// <summary>
    /// Чистая математика раскладки лута: выбор угловых слотов и проверки
    /// расстояния/разлёта. Без NavMesh и Random — тестируется в EditMode.
    /// </summary>
    public static class LootScatter
    {
        public const int AngleSlots = 36;

        /// Приоритетный пул: слоты, не заблокированные и ещё не занятые.
        public static List<int> BuildAnglePool(HashSet<int> blocked, HashSet<int> succeeded, int slotCount)
        {
            var pool = new List<int>(slotCount);
            for (int i = 0; i < slotCount; i++)
            {
                if ((blocked == null || !blocked.Contains(i)) &&
                    (succeeded == null || !succeeded.Contains(i)))
                    pool.Add(i);
            }

            return pool;
        }

        /// Точка на окружности разлёта для углового слота [0..slotCount).
        public static Vector3 CandidateAtAngle(Vector3 originFlat, float distance, int angleSlot, int slotCount)
        {
            float yaw = angleSlot * (360f / slotCount);
            Vector3 dir = Quaternion.AngleAxis(yaw, Vector3.up) * Vector3.forward;
            return originFlat + dir * distance;
        }

        /// Точка не дальше радиуса разлёта (с допуском на сэмпл NavMesh), по XZ.
        public static bool IsWithinScatter(Vector3 originFlat, Vector3 point, float maxScatter, float tolerance)
        {
            float dx = point.x - originFlat.x;
            float dz = point.z - originFlat.z;
            return Mathf.Sqrt(dx * dx + dz * dz) <= maxScatter + tolerance;
        }

        /// Точка не ближе separation к уже разложенному в этой волне (по XZ).
        public static bool IsFarFromPlaced(Vector3 point, IList<Vector3> placed, float separation)
        {
            if (separation <= 0f || placed == null)
                return true;

            float sq = separation * separation;
            for (int i = 0; i < placed.Count; i++)
            {
                Vector3 q = placed[i];
                float ox = point.x - q.x;
                float oz = point.z - q.z;
                if (ox * ox + oz * oz < sq)
                    return false;
            }

            return true;
        }
    }
}
