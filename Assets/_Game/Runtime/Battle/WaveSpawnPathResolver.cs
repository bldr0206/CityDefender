using System;
using System.Collections.Generic;
using ColorChargeTD.Data;

namespace ColorChargeTD.Battle
{
    public static class WaveSpawnPathResolver
    {
        public static Dictionary<string, LevelPathRuntimeDefinition> BuildPathIndex(LevelLayoutRuntimeDefinition layout)
        {
            Dictionary<string, LevelPathRuntimeDefinition> pathsById =
                new Dictionary<string, LevelPathRuntimeDefinition>(StringComparer.Ordinal);

            LevelPathRuntimeDefinition[] paths = layout.Paths;
            if (paths == null)
            {
                return pathsById;
            }

            for (int i = 0; i < paths.Length; i++)
            {
                LevelPathRuntimeDefinition path = paths[i];
                if (!string.IsNullOrWhiteSpace(path.PathId))
                {
                    pathsById[path.PathId] = path;
                }
            }

            return pathsById;
        }

        public static LevelPathRuntimeDefinition Resolve(
            WaveSpawnGroup group,
            LevelLayoutRuntimeDefinition layout,
            Dictionary<string, LevelPathRuntimeDefinition> pathsById)
        {
            if (!string.IsNullOrWhiteSpace(group.PathId) &&
                pathsById != null &&
                pathsById.TryGetValue(group.PathId, out LevelPathRuntimeDefinition byId))
            {
                return byId;
            }

            LevelPathRuntimeDefinition[] paths = layout.Paths;
            if (paths != null && paths.Length > 0)
            {
                return paths[0];
            }

            return default;
        }
    }
}
