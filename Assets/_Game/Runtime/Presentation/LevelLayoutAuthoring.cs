using System;
using UnityEngine;

namespace ColorChargeTD.Presentation
{
    public sealed class LevelLayoutAuthoring : MonoBehaviour
    {
        [SerializeField] private string layoutId = "layout-mvp-01";
        [SerializeField] private PathAuthoring[] paths = Array.Empty<PathAuthoring>();
        [SerializeField] private BuildSlotAuthoring[] buildSlots = Array.Empty<BuildSlotAuthoring>();
        [SerializeField] private BaseTargetAuthoring baseTarget;
        [SerializeField] private bool autoCollectChildren = true;

        #region Authoring
        private void OnValidate()
        {
            if (!autoCollectChildren)
            {
                return;
            }

            paths = GetComponentsInChildren<PathAuthoring>(true);
            buildSlots = GetComponentsInChildren<BuildSlotAuthoring>(true);
            baseTarget = GetComponentInChildren<BaseTargetAuthoring>(true);
        }
        #endregion

        #region RuntimeBuild
        public bool TryBuildDefinition(out Data.LevelLayoutRuntimeDefinition definition, out string error)
        {
            error = string.Empty;

            if (paths == null || paths.Length == 0)
            {
                definition = default;
                error = "Level layout needs at least one path.";
                return false;
            }

            if (buildSlots == null || buildSlots.Length == 0)
            {
                definition = default;
                error = "Level layout needs at least one build slot.";
                return false;
            }

            if (baseTarget == null)
            {
                definition = default;
                error = "Level layout needs a base target.";
                return false;
            }

            Data.LevelPathRuntimeDefinition[] pathDefinitions = new Data.LevelPathRuntimeDefinition[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                if (!paths[i].TryBuildDefinition(out Data.LevelPathRuntimeDefinition pathDefinition, out error))
                {
                    definition = default;
                    return false;
                }

                pathDefinitions[i] = pathDefinition;
            }

            Data.BuildSlotRuntimeDefinition[] slotDefinitions = new Data.BuildSlotRuntimeDefinition[buildSlots.Length];
            for (int i = 0; i < buildSlots.Length; i++)
            {
                slotDefinitions[i] = buildSlots[i].BuildDefinition();
            }

            definition = new Data.LevelLayoutRuntimeDefinition(layoutId, pathDefinitions, slotDefinitions, baseTarget.transform.position);
            return true;
        }
        #endregion
    }
}
