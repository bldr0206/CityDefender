using UnityEngine;

namespace Multitool.PrefabLocker
{
    /// <summary>
    /// Отменяет скрытие объекта его предками с PrefabLocker,
    /// оставляя его (и его потомков) видимыми в иерархии.
    /// </summary>
    [ExecuteAlways]
    public sealed class PrefabLockerForceVisible : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnEnable() => PrefabLockerVisibility.RefreshSoon();

        private void OnDisable() => PrefabLockerVisibility.RefreshSoon();

        private void OnValidate() => PrefabLockerVisibility.RefreshSoon();
#endif
    }
}
