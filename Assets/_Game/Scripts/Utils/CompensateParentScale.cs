using UnityEngine;

public class CompensateParentScale : MonoBehaviour
{
    private const float MinParentScale = 1e-5f;

    [SerializeField] private Vector3 _targetScale = Vector3.one;

    private void Start()
    {
        Apply();
    }

    private void OnDrawGizmos()
    {
        Apply();
    }

    private void Apply()
    {
        Transform parent = transform.parent;
        if (parent == null)
            return;

        Vector3 p = parent.lossyScale;
        transform.localScale = new Vector3(
            _targetScale.x / SafeParentAxis(p.x),
            _targetScale.y / SafeParentAxis(p.y),
            _targetScale.z / SafeParentAxis(p.z));
    }

    private static float SafeParentAxis(float axis) =>
        Mathf.Abs(axis) < MinParentScale ? 1f : axis;
}
