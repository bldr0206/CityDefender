using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Utils.Procedural
{
    /// <summary>
    /// Минимальный "экструдер" для низкополигональной оболочки из quad-граней.
    /// Держит только внешние грани, операции (extrude/split) сохраняют водонепроницаемость (без дыр).
    /// На выходе строит Mesh с flat-нормалями (вершины дублируются на грань).
    /// </summary>
    public sealed class QuadShellMeshBuilder
    {
        public readonly struct FaceId
        {
            public readonly int Value;
            public FaceId(int value) => Value = value;
            public override string ToString() => Value.ToString();
        }

        public readonly struct BoxFaces
        {
            public readonly FaceId Top;
            public readonly FaceId Bottom;
            public readonly FaceId Left;
            public readonly FaceId Right;
            public readonly FaceId Front;
            public readonly FaceId Back;

            public BoxFaces(FaceId top, FaceId bottom, FaceId left, FaceId right, FaceId front, FaceId back)
            {
                Top = top;
                Bottom = bottom;
                Left = left;
                Right = right;
                Front = front;
                Back = back;
            }
        }

        private struct Face
        {
            public Vector3 A;
            public Vector3 B;
            public Vector3 C;
            public Vector3 D;
            public bool Alive;

            public Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                A = a;
                B = b;
                C = c;
                D = d;
                Alive = true;
            }

            public Vector3 Center => (A + B + C + D) * 0.25f;
            public Vector3 Normal
            {
                get
                {
                    Vector3 ab = B - A;
                    Vector3 ad = D - A;
                    Vector3 n = Vector3.Cross(ab, ad);
                    float mag = n.magnitude;
                    return mag > 1e-8f ? (n / mag) : Vector3.up;
                }
            }
        }

        private readonly List<Face> _faces = new();

        public void Clear() => _faces.Clear();

        public readonly struct FaceInfo
        {
            public readonly FaceId Id;
            public readonly Vector3 Center;
            public readonly Vector3 Normal;

            public FaceInfo(FaceId id, Vector3 center, Vector3 normal)
            {
                Id = id;
                Center = center;
                Normal = normal;
            }
        }

        public IEnumerable<FaceInfo> EnumerateAliveFaces()
        {
            for (int i = 0; i < _faces.Count; i++)
            {
                Face f = _faces[i];
                if (!f.Alive) continue;
                yield return new FaceInfo(new FaceId(i), f.Center, f.Normal);
            }
        }

        public bool TryFindFace(Vector3 normalHint, float targetY, float yWeight, out FaceId faceId)
        {
            float hintMag = normalHint.magnitude;
            Vector3 hint = hintMag > 1e-8f ? (normalHint / hintMag) : Vector3.right;
            yWeight = Mathf.Max(0f, yWeight);

            bool found = false;
            float bestScore = float.NegativeInfinity;
            int best = -1;

            for (int i = 0; i < _faces.Count; i++)
            {
                Face f = _faces[i];
                if (!f.Alive) continue;

                float dot = Vector3.Dot(f.Normal, hint); // [-1..1]
                float dy = Mathf.Abs(f.Center.y - targetY);
                float score = dot - dy * yWeight;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                    found = true;
                }
            }

            faceId = found ? new FaceId(best) : default;
            return found;
        }

        public readonly struct ExtrudeResult
        {
            public readonly FaceId Cap;
            public readonly FaceId SideAB;
            public readonly FaceId SideBC;
            public readonly FaceId SideCD;
            public readonly FaceId SideDA;

            public ExtrudeResult(FaceId cap, FaceId sideAB, FaceId sideBC, FaceId sideCD, FaceId sideDA)
            {
                Cap = cap;
                SideAB = sideAB;
                SideBC = sideBC;
                SideCD = sideCD;
                SideDA = sideDA;
            }
        }

        public bool TryGetFaceInfo(FaceId id, out FaceInfo info)
        {
            if (id.Value < 0 || id.Value >= _faces.Count)
            {
                info = default;
                return false;
            }

            Face f = _faces[id.Value];
            if (!f.Alive)
            {
                info = default;
                return false;
            }

            info = new FaceInfo(id, f.Center, f.Normal);
            return true;
        }

        public BoxFaces AddBox(Vector3 center, Vector3 size)
        {
            Vector3 e = size * 0.5f;

            // 8 corners
            Vector3 p000 = center + new Vector3(-e.x, -e.y, -e.z);
            Vector3 p001 = center + new Vector3(-e.x, -e.y, +e.z);
            Vector3 p010 = center + new Vector3(-e.x, +e.y, -e.z);
            Vector3 p011 = center + new Vector3(-e.x, +e.y, +e.z);
            Vector3 p100 = center + new Vector3(+e.x, -e.y, -e.z);
            Vector3 p101 = center + new Vector3(+e.x, -e.y, +e.z);
            Vector3 p110 = center + new Vector3(+e.x, +e.y, -e.z);
            Vector3 p111 = center + new Vector3(+e.x, +e.y, +e.z);

            // Важно: порядок вершин по часовой стрелке, глядя СНАРУЖИ (правило правой руки для нормали наружу)
            // Top (+Y): p010 p110 p111 p011
            FaceId top = AddFace(new Face(p010, p110, p111, p011));
            // Bottom (-Y): p100 p000 p001 p101
            FaceId bottom = AddFace(new Face(p100, p000, p001, p101));
            // Left (-X): p000 p010 p011 p001
            FaceId left = AddFace(new Face(p000, p010, p011, p001));
            // Right (+X): p110 p100 p101 p111
            FaceId right = AddFace(new Face(p110, p100, p101, p111));
            // Front (+Z): p001 p011 p111 p101
            FaceId front = AddFace(new Face(p001, p011, p111, p101));
            // Back (-Z): p100 p110 p010 p000
            FaceId back = AddFace(new Face(p100, p110, p010, p000));

            return new BoxFaces(top, bottom, left, right, front, back);
        }

        public FaceId Extrude(FaceId faceId, float distance, float capScale = 1f)
        {
            if (distance <= 0f) throw new ArgumentOutOfRangeException(nameof(distance), "distance must be > 0");
            if (capScale <= 0f) throw new ArgumentOutOfRangeException(nameof(capScale), "capScale must be > 0");

            Face baseFace = GetAlive(faceId);
            Remove(faceId);

            Vector3 n = baseFace.Normal;
            Vector3 center = baseFace.Center;

            Vector3 a2 = center + (baseFace.A - center) * capScale + n * distance;
            Vector3 b2 = center + (baseFace.B - center) * capScale + n * distance;
            Vector3 c2 = center + (baseFace.C - center) * capScale + n * distance;
            Vector3 d2 = center + (baseFace.D - center) * capScale + n * distance;

            // 4 боковых грани: следим за внешней ориентацией.
            // Каждая боковина должна иметь нормаль наружу. Для исходной грани (A,B,C,D) порядок наружу задан.
            AddFace(new Face(baseFace.A, baseFace.B, b2, a2));
            AddFace(new Face(baseFace.B, baseFace.C, c2, b2));
            AddFace(new Face(baseFace.C, baseFace.D, d2, c2));
            AddFace(new Face(baseFace.D, baseFace.A, a2, d2));

            // Новый "кэп" - это параллельная грань. Ориентация наружу совпадает с baseFace.
            FaceId cap = AddFace(new Face(a2, b2, c2, d2));
            return cap;
        }

        public ExtrudeResult ExtrudeWithSides(FaceId faceId, float distance, float capScale = 1f)
        {
            if (distance <= 0f) throw new ArgumentOutOfRangeException(nameof(distance), "distance must be > 0");
            if (capScale <= 0f) throw new ArgumentOutOfRangeException(nameof(capScale), "capScale must be > 0");

            Face baseFace = GetAlive(faceId);
            Remove(faceId);

            Vector3 n = baseFace.Normal;
            Vector3 center = baseFace.Center;

            Vector3 a2 = center + (baseFace.A - center) * capScale + n * distance;
            Vector3 b2 = center + (baseFace.B - center) * capScale + n * distance;
            Vector3 c2 = center + (baseFace.C - center) * capScale + n * distance;
            Vector3 d2 = center + (baseFace.D - center) * capScale + n * distance;

            FaceId sideAB = AddFace(new Face(baseFace.A, baseFace.B, b2, a2));
            FaceId sideBC = AddFace(new Face(baseFace.B, baseFace.C, c2, b2));
            FaceId sideCD = AddFace(new Face(baseFace.C, baseFace.D, d2, c2));
            FaceId sideDA = AddFace(new Face(baseFace.D, baseFace.A, a2, d2));

            FaceId cap = AddFace(new Face(a2, b2, c2, d2));
            return new ExtrudeResult(cap, sideAB, sideBC, sideCD, sideDA);
        }

        /// <summary>
        /// Делит грань на 2 квада по направлению AB (между ребрами AD и BC).
        /// Полезно для "таза": нижнюю грань делим на левую/правую под ноги.
        /// Возвращает (left, right) в локальном смысле грани (по направлению A->B).
        /// </summary>
        public (FaceId first, FaceId second) SplitAlongAB(FaceId faceId, float t = 0.5f)
        {
            if (t <= 0f || t >= 1f) throw new ArgumentOutOfRangeException(nameof(t), "t must be in (0,1)");

            Face f = GetAlive(faceId);
            Remove(faceId);

            // Режем по ребрам AD и BC на одинаковом t вдоль A->B направления (т.е. между AD и BC):
            // Берём точки на AB и DC? Нет: надо разделить прямоугольник поперёк, т.е. вставить ребро параллельно AD/BC.
            // Поэтому берём точки на AB и DC в одинаковой пропорции t.
            Vector3 e0 = Vector3.Lerp(f.A, f.B, t);
            Vector3 e1 = Vector3.Lerp(f.D, f.C, t);

            // Quad1: A -> e0 -> e1 -> D
            FaceId q1 = AddFace(new Face(f.A, e0, e1, f.D));
            // Quad2: e0 -> B -> C -> e1
            FaceId q2 = AddFace(new Face(e0, f.B, f.C, e1));

            return (q1, q2);
        }

        public Mesh ToMesh(string name = "ProceduralMesh")
        {
            var vertices = new List<Vector3>(_faces.Count * 4);
            var normals = new List<Vector3>(_faces.Count * 4);
            var triangles = new List<int>(_faces.Count * 6);

            for (int i = 0; i < _faces.Count; i++)
            {
                Face f = _faces[i];
                if (!f.Alive) continue;

                int vBase = vertices.Count;
                vertices.Add(f.A);
                vertices.Add(f.B);
                vertices.Add(f.C);
                vertices.Add(f.D);

                Vector3 n = f.Normal;
                normals.Add(n);
                normals.Add(n);
                normals.Add(n);
                normals.Add(n);

                // Два треугольника: (A,B,C) и (A,C,D) при CW-ориентации наружу.
                triangles.Add(vBase + 0);
                triangles.Add(vBase + 1);
                triangles.Add(vBase + 2);
                triangles.Add(vBase + 0);
                triangles.Add(vBase + 2);
                triangles.Add(vBase + 3);
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            return mesh;
        }

        private FaceId AddFace(Face face)
        {
            _faces.Add(face);
            return new FaceId(_faces.Count - 1);
        }

        private Face GetAlive(FaceId id)
        {
            if (id.Value < 0 || id.Value >= _faces.Count)
                throw new ArgumentOutOfRangeException(nameof(id), $"FaceId out of range: {id.Value}");

            Face f = _faces[id.Value];
            if (!f.Alive)
                throw new InvalidOperationException($"FaceId is not alive: {id.Value}");

            return f;
        }

        private void Remove(FaceId id)
        {
            if (id.Value < 0 || id.Value >= _faces.Count)
                throw new ArgumentOutOfRangeException(nameof(id), $"FaceId out of range: {id.Value}");

            Face f = _faces[id.Value];
            if (!f.Alive) return;
            f.Alive = false;
            _faces[id.Value] = f;
        }
    }
}

