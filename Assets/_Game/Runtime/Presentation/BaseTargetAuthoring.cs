using UnityEngine;

namespace ColorChargeTD.Presentation
{
    public sealed class BaseTargetAuthoring : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.35f, 0.85f);
            Gizmos.DrawCube(transform.position + Vector3.up * 0.25f, new Vector3(0.7f, 0.5f, 0.7f));
        }
    }
}
