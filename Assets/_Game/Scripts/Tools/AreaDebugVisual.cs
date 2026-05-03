using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AreaDebugVisual : MonoBehaviour
{
    #region Fields

    [SerializeField] Color color = new Color(0.5f, 1f, 0f, 0.05f);
    [SerializeField] bool draw = true;

    Collider _collider;

    static Mesh _capsuleMesh;
    static Mesh _sphereMesh;

    #endregion

    #region Gizmos

    void OnDrawGizmos()
    {
        if (!draw) return;

        if (_collider == null)
            _collider = GetComponent<Collider>();

        if (_collider == null) return;

        Gizmos.color = color;

        switch (_collider)
        {
            case BoxCollider box:
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                break;

            case SphereCollider sphere:
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawMesh(SphereMesh, sphere.center, Quaternion.identity,
                    Vector3.one * (sphere.radius * 2f));
                break;

            case CapsuleCollider capsule:
                DrawCapsuleGizmo(capsule);
                break;

            case MeshCollider meshCol when meshCol.sharedMesh != null:
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawMesh(meshCol.sharedMesh);
                break;
        }
    }

    void DrawCapsuleGizmo(CapsuleCollider capsule)
    {
        if (CapsuleMesh == null) return;

        // Primitive capsule mesh: height=2, radius=0.5 (along Y)
        float effectiveHeight = Mathf.Max(capsule.height, capsule.radius * 2f);
        float diameter = capsule.radius * 2f;

        Vector3 localScale = capsule.direction switch
        {
            0 => new Vector3(effectiveHeight / 2f, diameter, diameter),
            2 => new Vector3(diameter, diameter, effectiveHeight / 2f),
            _ => new Vector3(diameter, effectiveHeight / 2f, diameter),
        };

        Quaternion dirRotation = capsule.direction switch
        {
            0 => Quaternion.Euler(0f, 0f, 90f),
            2 => Quaternion.Euler(90f, 0f, 0f),
            _ => Quaternion.identity,
        };

        var worldCenter = transform.TransformPoint(capsule.center);
        var worldScale = Vector3.Scale(localScale, transform.lossyScale);
        var worldRotation = transform.rotation * dirRotation;

        Gizmos.matrix = Matrix4x4.TRS(worldCenter, worldRotation, worldScale);
        Gizmos.DrawMesh(CapsuleMesh);
    }

    #endregion

    #region Mesh Cache

    static Mesh CapsuleMesh
    {
        get
        {
            if (_capsuleMesh != null) return _capsuleMesh;
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _capsuleMesh = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);
            return _capsuleMesh;
        }
    }

    static Mesh SphereMesh
    {
        get
        {
            if (_sphereMesh != null) return _sphereMesh;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _sphereMesh = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);
            return _sphereMesh;
        }
    }

    #endregion
}
