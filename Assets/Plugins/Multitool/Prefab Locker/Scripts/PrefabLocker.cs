using UnityEngine;

namespace Multitool.PrefabLocker
{
    /// <summary>
    /// Блокирует отображение детей префаба в иерархии сцены.
    /// Дети остаются видимыми только при редактировании самого префаба.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class PrefabLocker : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnEnable() => PrefabLockerVisibility.RefreshSoon();

        private void OnDisable()
        {
            PrefabLockerVisibility.UnhideBranch(transform);
            PrefabLockerVisibility.RefreshSoon();
        }

        private void OnDestroy()
        {
            PrefabLockerVisibility.UnhideBranch(transform);
            PrefabLockerVisibility.RefreshSoon();
        }

        private void OnTransformChildrenChanged() => PrefabLockerVisibility.RefreshSoon();

        private void OnValidate() => PrefabLockerVisibility.RefreshSoon();
#endif
    }
}
