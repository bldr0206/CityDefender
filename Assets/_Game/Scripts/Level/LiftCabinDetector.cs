using UnityEngine;

/// <summary>
/// Дочерний триггер кабины лифта: пробрасывает вход/выход в <see cref="Lift"/> (на том же объекте или родителе).
/// Учитывается коллайдер с тегом <b>Contact</b> (как у игрока для <see cref="PlayerContact"/>).
/// Если коллайдер Is Trigger висит на том же GameObject, что и <see cref="Lift"/>, этот компонент не нужен.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class LiftCabinDetector : MonoBehaviour
{
    [SerializeField] Lift _lift;

    void Awake()
    {
        if (_lift == null)
            _lift = GetComponentInParent<Lift>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCabinPresenceCollider(other) || _lift == null)
            return;

        _lift.NotifyPlayerEnteredCabin();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCabinPresenceCollider(other) || _lift == null)
            return;

        _lift.NotifyPlayerLeftCabin();
    }

    static bool IsPlayerCabinPresenceCollider(Collider other) =>
        other != null && other.CompareTag(GameTags.Contact);
}
