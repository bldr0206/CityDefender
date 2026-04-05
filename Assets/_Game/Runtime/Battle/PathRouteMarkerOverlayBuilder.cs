using System.Collections.Generic;
using ColorChargeTD.Data;
using ColorChargeTD.Domain;
using UnityEngine;

namespace ColorChargeTD.Battle
{
    public static class PathRouteMarkerOverlayBuilder
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int CullId = Shader.PropertyToID("_Cull");

        #region Public API
        public static void Build(
            Transform parent,
            GameObject markerPrefab,
            LevelLayoutRuntimeDefinition layout,
            WaveDefinition wave,
            float spacing,
            float yOffset,
            MaterialPropertyBlock propertyBlock)
        {
            if (parent == null || markerPrefab == null || layout.Paths == null)
            {
                return;
            }

            if (wave == null || wave.Groups == null || wave.Groups.Count == 0)
            {
                return;
            }

            Dictionary<string, LevelPathRuntimeDefinition> pathsById = WaveSpawnPathResolver.BuildPathIndex(layout);
            Dictionary<string, List<ColorCharge>> palettes = BuildPalettesPerPath(layout, wave, pathsById);

            LevelPathRuntimeDefinition[] paths = layout.Paths;
            for (int i = 0; i < paths.Length; i++)
            {
                LevelPathRuntimeDefinition path = paths[i];
                if (string.IsNullOrWhiteSpace(path.PathId))
                {
                    continue;
                }

                if (!palettes.TryGetValue(path.PathId, out List<ColorCharge> palette) || palette == null || palette.Count == 0)
                {
                    continue;
                }

                Vector3[] waypoints = path.Waypoints;
                if (waypoints == null || waypoints.Length < 2)
                {
                    continue;
                }

                PlaceMarkersAlongPath(parent, markerPrefab, waypoints, palette, spacing, yOffset, propertyBlock);
            }
        }
        #endregion

        #region Palettes
        private static Dictionary<string, List<ColorCharge>> BuildPalettesPerPath(
            LevelLayoutRuntimeDefinition layout,
            WaveDefinition wave,
            Dictionary<string, LevelPathRuntimeDefinition> pathsById)
        {
            Dictionary<string, List<ColorCharge>> palettes =
                new Dictionary<string, List<ColorCharge>>(System.StringComparer.Ordinal);

            LevelPathRuntimeDefinition[] paths = layout.Paths;
            for (int i = 0; i < paths.Length; i++)
            {
                LevelPathRuntimeDefinition p = paths[i];
                if (!string.IsNullOrWhiteSpace(p.PathId))
                {
                    palettes[p.PathId] = new List<ColorCharge>();
                }
            }

            for (int g = 0; g < wave.Groups.Count; g++)
            {
                WaveSpawnGroup group = wave.Groups[g];
                if (group.Enemy == null)
                {
                    continue;
                }

                LevelPathRuntimeDefinition resolved = WaveSpawnPathResolver.Resolve(group, layout, pathsById);
                if (string.IsNullOrWhiteSpace(resolved.PathId))
                {
                    continue;
                }

                if (!palettes.TryGetValue(resolved.PathId, out List<ColorCharge> list))
                {
                    continue;
                }

                ColorCharge c = group.Enemy.Color;
                if (!list.Contains(c))
                {
                    list.Add(c);
                }
            }

            return palettes;
        }
        #endregion

        #region Placement
        private static void PlaceMarkersAlongPath(
            Transform parent,
            GameObject markerPrefab,
            Vector3[] waypoints,
            List<ColorCharge> palette,
            float spacing,
            float yOffset,
            MaterialPropertyBlock propertyBlock)
        {
            float totalLength = PolylineLength(waypoints);
            if (totalLength < 0.001f)
            {
                return;
            }

            float step = Mathf.Max(0.05f, spacing);
            List<float> distances = new List<float>();
            for (float d = step * 0.5f; d < totalLength; d += step)
            {
                distances.Add(d);
            }

            if (distances.Count == 0)
            {
                distances.Add(totalLength * 0.5f);
            }

            for (int i = 0; i < distances.Count; i++)
            {
                if (!TrySamplePolyline(waypoints, distances[i], totalLength, out Vector3 position, out Vector3 tangent))
                {
                    continue;
                }

                position.y += yOffset;
                Vector3 dir = tangent.sqrMagnitude >= 0.0001f ? tangent.normalized : Vector3.forward;
                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                Quaternion rotation = Quaternion.Euler(90f, yaw, 0f);
                GameObject instance = Object.Instantiate(markerPrefab, position, rotation, parent);

                ColorCharge charge = palette[i % palette.Count];
                ApplyMarkerColor(instance, charge.ToUnityColor(), propertyBlock);
            }
        }

        private static void ApplyMarkerColor(GameObject instance, Color color, MaterialPropertyBlock block)
        {
            if (instance == null || block == null)
            {
                return;
            }

            MeshRenderer renderer = instance.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = instance.GetComponentInChildren<MeshRenderer>();
            }

            if (renderer == null)
            {
                return;
            }

            block.Clear();
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(CullId))
            {
                block.SetFloat(CullId, 0f);
            }

            renderer.SetPropertyBlock(block);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.staticShadowCaster = false;
        }

        private static float PolylineLength(Vector3[] points)
        {
            float sum = 0f;
            for (int i = 0; i < points.Length - 1; i++)
            {
                sum += Vector3.Distance(points[i], points[i + 1]);
            }

            return sum;
        }

        private static bool TrySamplePolyline(
            Vector3[] points,
            float distanceAlong,
            float totalLength,
            out Vector3 position,
            out Vector3 tangent)
        {
            position = points[0];
            tangent = points.Length > 1 ? points[1] - points[0] : Vector3.forward;

            if (distanceAlong <= 0f)
            {
                tangent = points.Length > 1 ? points[1] - points[0] : Vector3.forward;
                return true;
            }

            float traveled = 0f;
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];
                Vector3 seg = b - a;
                float segLen = seg.magnitude;
                if (segLen < 0.0001f)
                {
                    continue;
                }

                if (traveled + segLen >= distanceAlong - 0.0001f)
                {
                    float t = (distanceAlong - traveled) / segLen;
                    t = Mathf.Clamp01(t);
                    position = Vector3.Lerp(a, b, t);
                    tangent = seg;
                    return true;
                }

                traveled += segLen;
            }

            position = points[points.Length - 1];
            tangent = points.Length >= 2 ? points[points.Length - 1] - points[points.Length - 2] : Vector3.forward;
            return distanceAlong <= totalLength + 0.01f;
        }
        #endregion
    }
}
