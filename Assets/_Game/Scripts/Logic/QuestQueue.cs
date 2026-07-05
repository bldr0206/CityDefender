using System.Collections.Generic;

namespace CityDef.Gameplay.Logic
{
    /// <summary>
    /// Чистая логика очереди квестов: выбор следующего невыполненного по порядку.
    /// Работает со строковыми id — тестируется в EditMode без сцены.
    /// </summary>
    public static class QuestQueue
    {
        /// <summary>
        /// Первый id из <paramref name="orderedIds"/>, которого нет в <paramref name="completedIds"/>,
        /// или null, если все выполнены (либо список пуст).
        /// </summary>
        public static string NextIncomplete(IReadOnlyList<string> orderedIds, ICollection<string> completedIds)
        {
            if (orderedIds == null)
                return null;

            for (int i = 0; i < orderedIds.Count; i++)
            {
                string id = orderedIds[i];
                if (completedIds == null || !completedIds.Contains(id))
                    return id;
            }

            return null;
        }
    }
}
