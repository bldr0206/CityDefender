using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Зона перед обрывом: когда игрок (Rigidbody, тег Player) пересекает триггер, нанятые боты идут к Edge по NavMesh, затем движение по дуге к Landing.
/// Если бот уже идёт к обрыву или в прыжке, следующий обрыв ставится в очередь и выполняется после приземления.
///
/// Настройка в Unity:
/// 1. Пустой объект (например CliffJumpZone) у траектории игрока перед обрывом.
/// 2. Collider → Is Trigger, размер чтобы игрок проходил насквозь; слои в Edit → Project Settings → Physics должны пересекаться с коллайдером игрока.
/// 3. Этот компонент на том же объекте.
/// 4. Дочерний объект Edge — на запечённом NavMesh у самого края обрыва.
/// 5. Дочерний объект Landing — на NavMesh платформы, куда приземляется прыжок.
/// 6. В инспекторе задать Edge, Landing, Jump Speed, Jump Power; Fire Once — один раз за «заход»; при Reset On Player Exit игрок должен выйти из коллайдера и снова войти, чтобы зона сработала снова.
/// 7. Draw Jump Path In Scene — приблизительная кривая траектории прыжка в Scene View.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BotCliffJumpZone : MonoBehaviour
{
    static readonly Color JumpPathGizmoColor = new Color(0.2f, 0.95f, 1f, 0.85f);

    [SerializeField] Transform _edge;
    [SerializeField] Transform _landing;

    [SerializeField] float _jumpSpeed = 8f;
    [SerializeField] float _jumpPower = 3f;
    [SerializeField, Min(1)] int _numJumps = 1;

    [SerializeField] bool _fireOnce = true;

    [Tooltip("Если включено с Fire Once: сбрасывать «уже срабатывало», когда игрок выходит из триггера, чтобы со второго захода снова вызвать прыжок.")]
    [SerializeField] bool _resetFireOnceOnPlayerExit = true;

    [SerializeField] bool _drawJumpPathInScene = true;
    [SerializeField, Min(4)] int _jumpPathSegments = 48;

    bool _hasFiredForPlayerCrossing;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!_drawJumpPathInScene || _edge == null || _landing == null)
            return;

        Vector3 start = _edge.position;
        Vector3 end = _landing.position;

        Gizmos.color = JumpPathGizmoColor;
        Gizmos.DrawWireSphere(start, 0.15f);
        Gizmos.DrawWireSphere(end, 0.15f);

        int n = Mathf.Max(1, _numJumps);
        int segments = Mathf.Max(4, _jumpPathSegments);

        Vector3 prev = SampleApproxJumpPath(start, end, _jumpPower, n, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float u = i / (float)segments;
            Vector3 p = SampleApproxJumpPath(start, end, _jumpPower, n, u);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }

    /// <summary>
    /// Приближение траектории DOJump: XZ движутся вдоль хорды по сегментам, по Y добавляется numJumps дуг с пиком jumpPower.
    /// </summary>
    static Vector3 SampleApproxJumpPath(Vector3 start, Vector3 end, float jumpPower, int numJumps, float u)
    {
        numJumps = Mathf.Max(1, numJumps);
        float segF = u * numJumps;
        int seg = Mathf.Min(Mathf.FloorToInt(segF), numJumps - 1);
        float localT = segF - seg;

        Vector3 segA = Vector3.Lerp(start, end, seg / (float)numJumps);
        Vector3 segB = Vector3.Lerp(start, end, (seg + 1) / (float)numJumps);
        Vector3 p = Vector3.Lerp(segA, segB, localT);
        p.y += jumpPower * 4f * localT * (1f - localT);
        return p;
    }
#endif

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        if (!Game.HasHiredBots)
            return;

        if (_fireOnce && _hasFiredForPlayerCrossing)
            return;

        if (_edge == null || _landing == null)
        {
            Debug.LogWarning($"{nameof(BotCliffJumpZone)}: Edge или Landing не назначены.", this);
            return;
        }

        bool anyCliffJumpStarted = false;

        List<Bot> bots = SaveableRegistry.GetAll<Bot>();
        for (int i = 0; i < bots.Count; i++)
        {
            Bot bot = bots[i];
            if (bot == null || !bot.isActiveAndEnabled)
                continue;
            if (bot.IsInLiftBoardingOrRide)
                continue;

            bot.BeginCliffJump(
                _edge.position,
                _landing.position,
                _landing.rotation,
                _jumpSpeed,
                _jumpPower,
                _numJumps);
            if (bot.IsInCliffJump)
                anyCliffJumpStarted = true;
        }

        if (_fireOnce && anyCliffJumpStarted)
            _hasFiredForPlayerCrossing = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!_fireOnce || !_resetFireOnceOnPlayerExit)
            return;
        if (!IsPlayer(other))
            return;
        _hasFiredForPlayerCrossing = false;
    }

    static bool IsPlayer(Collider other)
    {
        return other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(GameTags.Player);
    }
}
