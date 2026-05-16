using UnityEngine;

/// <summary>
/// Зона вызова с этажа: игрок с тегом Contact входит в коллайдер с тегом <b>LiftTrigger</b> — лифт подъезжает с другого этажа (см. <see cref="PlayerContact"/>).
/// Кнопка «ехать» в кабине должна вызывать <see cref="Lift.MoveToOppositeFloor"/>, а не этот компонент.
/// </summary>
public class LiftTrigger : MonoBehaviour
{
    [SerializeField, Tooltip("Вкл. — зона наверху (MoveUp). Выкл. — зона внизу (MoveDown).")]
    bool isUpTrigger;

    [SerializeField] Lift lift;

    public void CallTheLift()
    {
        if (lift == null)
            return;

        if (isUpTrigger)
            lift.MoveUp();
        else
            lift.MoveDown();
    }
}
