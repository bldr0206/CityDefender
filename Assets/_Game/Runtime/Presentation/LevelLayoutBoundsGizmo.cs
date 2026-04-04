using UnityEngine;

namespace ColorChargeTD.Presentation
{
    public sealed class LevelLayoutBoundsGizmo : MonoBehaviour
    {
        [SerializeField] private bool showInSceneView = true;
        [SerializeField] private Color wireColor = new Color(0.25f, 0.85f, 1f, 0.95f);
        [SerializeField] private Color fillColor = new Color(0.25f, 0.85f, 1f, 0.06f);
        [SerializeField] private Vector3 localCenter;
        [SerializeField] private Vector3 localSize = new Vector3(18f, 0.5f, 14f);

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (!showInSceneView)
            {
                return;
            }

            Gizmos.matrix = transform.localToWorldMatrix;

            if (fillColor.a > 0f)
            {
                Gizmos.color = fillColor;
                Gizmos.DrawCube(localCenter, localSize);
            }

            Gizmos.color = wireColor;
            Gizmos.DrawWireCube(localCenter, localSize);
        }

        #endregion
    }
}
