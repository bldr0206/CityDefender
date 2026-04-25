using System;
using UnityEngine;

namespace _Game.Scripts.Utils.Procedural
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class StickmanMeshGenerator : MonoBehaviour
    {
        [Header("General")]
        [SerializeField] private string meshName = "Stickman";

        [Header("Head / Neck")]
        [SerializeField] private Vector3 headSize = new(0.6f, 0.6f, 0.6f);
        [SerializeField] private float neckLength = 0.25f;
        [SerializeField] private float neckScale = 0.6f;

        [Header("Torso (3 sectors)")]
        [SerializeField] private float chestLength = 0.5f;
        [SerializeField] private float bellyLength = 0.45f;
        [SerializeField] private float pelvisLength = 0.35f;

        [SerializeField] private float chestScale = 1.35f;
        [SerializeField] private float bellyScale = 1.25f;
        [SerializeField] private float pelvisScale = 1.1f;

        [Header("Arms")]
        [SerializeField] private float shoulderLength = 0.35f;
        [SerializeField] private float forearmLength = 0.35f;
        [SerializeField] private float handLength = 0.15f;
        [SerializeField] private float armCapScale = 0.85f;

        [Header("Legs")]
        [SerializeField] private float thighLength = 0.5f;
        [SerializeField] private float shinLength = 0.5f;
        [SerializeField] private float footLength = 0.2f;
        [SerializeField] private float legCapScale = 0.9f;

        [SerializeField] private float legSplitT = 0.5f;

        [ContextMenu("Generate Mesh")]
        public void GenerateMesh()
        {
            ValidateParams();

            var builder = new QuadShellMeshBuilder();

            // Вершины меша должны быть в ЛОКАЛЬНЫХ координатах объекта.
            // Ставим "ступни" примерно около Y=0, а всё тело уходит вверх.
            float groundY = 0f;
            float pelvisBottomY = groundY + footLength + shinLength + thighLength;
            float headCenterY = pelvisBottomY + pelvisLength + bellyLength + chestLength + neckLength + headSize.y * 0.5f;

            // Голова
            Vector3 headCenter = new Vector3(0f, headCenterY, 0f);
            QuadShellMeshBuilder.BoxFaces head = builder.AddBox(headCenter, headSize);

            // Шея (экструд нижней грани головы вниз)
            QuadShellMeshBuilder.FaceId neckCap = builder.Extrude(head.Bottom, neckLength, neckScale);

            // Туловище (3 сектора) - всё вниз
            QuadShellMeshBuilder.ExtrudeResult chest = builder.ExtrudeWithSides(neckCap, chestLength, chestScale);
            QuadShellMeshBuilder.FaceId bellyCap = builder.Extrude(chest.Cap, bellyLength, bellyScale);
            QuadShellMeshBuilder.FaceId pelvisCap = builder.Extrude(bellyCap, pelvisLength, pelvisScale);

            // Руки: берём боковые грани именно с сегмента "грудь" (детерминированно, без поиска по всей оболочке).
            BuildArms(builder, chest);

            // Ноги: нижнюю грань таза делим на две и экструзим вниз
            BuildLegs(builder, pelvisCap);

            Mesh mesh = builder.ToMesh(meshName);
            ValidateMesh(mesh);

            var filter = GetComponent<MeshFilter>();
            filter.sharedMesh = mesh;
        }

        private void BuildArms(QuadShellMeshBuilder builder, QuadShellMeshBuilder.ExtrudeResult chest)
        {
            // У экструда 4 боковины: выбираем те, что смотрят примерно в +/-X.
            QuadShellMeshBuilder.FaceId[] sides = { chest.SideAB, chest.SideBC, chest.SideCD, chest.SideDA };
            QuadShellMeshBuilder.FaceId? left = null;
            QuadShellMeshBuilder.FaceId? right = null;

            float bestL = -1f;
            float bestR = -1f;
            for (int i = 0; i < sides.Length; i++)
            {
                if (!builder.TryGetFaceInfo(sides[i], out var info)) continue;
                float dl = Vector3.Dot(info.Normal, Vector3.left);
                float dr = Vector3.Dot(info.Normal, Vector3.right);
                if (dl > bestL)
                {
                    bestL = dl;
                    left = sides[i];
                }
                if (dr > bestR)
                {
                    bestR = dr;
                    right = sides[i];
                }
            }

            // Чтобы не экструдить случайно "почти не боковину", ставим минимальный порог.
            if (bestL < 0.6f) left = null;
            if (bestR < 0.6f) right = null;

            if (left.HasValue)
            {
                QuadShellMeshBuilder.FaceId cap = builder.Extrude(left.Value, shoulderLength, armCapScale);
                cap = builder.Extrude(cap, forearmLength, armCapScale);
                builder.Extrude(cap, handLength, armCapScale);
            }

            if (right.HasValue)
            {
                QuadShellMeshBuilder.FaceId cap = builder.Extrude(right.Value, shoulderLength, armCapScale);
                cap = builder.Extrude(cap, forearmLength, armCapScale);
                builder.Extrude(cap, handLength, armCapScale);
            }
        }

        private void BuildLegs(QuadShellMeshBuilder builder, QuadShellMeshBuilder.FaceId pelvisBottomCap)
        {
            // pelvisBottomCap - это нижняя грань после Extrude(bellyCap -> pelvisCap), т.е. "кэп" таза.
            // Нам нужна "нижняя грань таза", из неё делаем 2 ноги. В нашей цепочке pelvisBottomCap является именно нижней гранью.
            var (leg1, leg2) = builder.SplitAlongAB(pelvisBottomCap, Mathf.Clamp01(legSplitT));

            QuadShellMeshBuilder.FaceId cap1 = builder.Extrude(leg1, thighLength, legCapScale);
            cap1 = builder.Extrude(cap1, shinLength, legCapScale);
            builder.Extrude(cap1, footLength, legCapScale);

            QuadShellMeshBuilder.FaceId cap2 = builder.Extrude(leg2, thighLength, legCapScale);
            cap2 = builder.Extrude(cap2, shinLength, legCapScale);
            builder.Extrude(cap2, footLength, legCapScale);
        }

        private void ValidateParams()
        {
            if (headSize.x <= 0f || headSize.y <= 0f || headSize.z <= 0f) throw new ArgumentOutOfRangeException(nameof(headSize));
            if (neckLength <= 0f) throw new ArgumentOutOfRangeException(nameof(neckLength));
            if (chestLength <= 0f || bellyLength <= 0f || pelvisLength <= 0f) throw new ArgumentOutOfRangeException("Torso lengths must be > 0");
            if (shoulderLength <= 0f || forearmLength <= 0f || handLength <= 0f) throw new ArgumentOutOfRangeException("Arm lengths must be > 0");
            if (thighLength <= 0f || shinLength <= 0f || footLength <= 0f) throw new ArgumentOutOfRangeException("Leg lengths must be > 0");

            if (neckScale <= 0f || chestScale <= 0f || bellyScale <= 0f || pelvisScale <= 0f) throw new ArgumentOutOfRangeException("Scales must be > 0");
            if (armCapScale <= 0f || legCapScale <= 0f) throw new ArgumentOutOfRangeException("Cap scales must be > 0");
            if (legSplitT <= 0f || legSplitT >= 1f) throw new ArgumentOutOfRangeException(nameof(legSplitT), "legSplitT must be in (0,1)");
        }

        private static void ValidateMesh(Mesh mesh)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));

            Vector3[] verts = mesh.vertices;
            if (verts == null || verts.Length == 0) throw new InvalidOperationException("Generated mesh has no vertices.");

            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                if (!IsFinite(v))
                    throw new InvalidOperationException($"Vertex {i} is not finite: {v}");
            }

            int[] tris = mesh.triangles;
            if (tris == null || tris.Length == 0) throw new InvalidOperationException("Generated mesh has no triangles.");
            if ((tris.Length % 3) != 0) throw new InvalidOperationException("Triangle index array is not divisible by 3.");

            for (int i = 0; i < tris.Length; i++)
            {
                int idx = tris[i];
                if (idx < 0 || idx >= verts.Length)
                    throw new InvalidOperationException($"Triangle index out of range at {i}: {idx} (verts: {verts.Length})");
            }

            Bounds b = mesh.bounds;
            if (!IsFinite(b.center) || !IsFinite(b.extents))
                throw new InvalidOperationException($"Mesh bounds are not finite: {b}");
        }

        private static bool IsFinite(Vector3 v)
        {
            return IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
        }

        private static bool IsFinite(float f)
        {
            return !float.IsNaN(f) && !float.IsInfinity(f);
        }
    }
}

