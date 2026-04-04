using System.Collections.Generic;
using UnityEngine;

namespace ColorChargeTD.Data
{
    [CreateAssetMenu(menuName = "Color Charge TD/Content/Wave Definition", fileName = "WaveDefinition")]
    public sealed class WaveDefinition : ScriptableObject
    {
        [SerializeField] private List<WaveSpawnGroup> groups = new List<WaveSpawnGroup>();
        [SerializeField] private bool chainGroupsWithoutPlayerAck;

        public IReadOnlyList<WaveSpawnGroup> Groups => groups;
        public bool ChainGroupsWithoutPlayerAck => chainGroupsWithoutPlayerAck;

        public float GetTotalDuration()
        {
            float total = 0f;

            for (int i = 0; i < groups.Count; i++)
            {
                WaveSpawnGroup group = groups[i];
                total += group.StartDelay;
                total += Mathf.Max(0, group.Count - 1) * group.SpawnInterval;
            }

            return total;
        }

        public int GetTotalPlannedEnemyCount()
        {
            int total = 0;
            for (int i = 0; i < groups.Count; i++)
            {
                total += Mathf.Max(0, groups[i].Count);
            }

            return total;
        }

        public void ValidateInto(List<ContentValidationMessage> messages)
        {
            if (groups.Count == 0)
            {
                messages.Add(ContentValidationMessage.Error(name, "Wave definition must contain at least one spawn group."));
            }

            for (int i = 0; i < groups.Count; i++)
            {
                WaveSpawnGroup group = groups[i];
                if (group.Enemy == null)
                {
                    messages.Add(ContentValidationMessage.Error(name, "Each spawn group must reference an enemy definition."));
                }

                if (group.Count <= 0)
                {
                    messages.Add(ContentValidationMessage.Error(name, "Spawn group count must be greater than zero."));
                }
            }
        }
    }
}
