using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Лифт: движение по вызову <see cref="MoveToOppositeFloor"/> / <see cref="MoveUp"/> / <see cref="MoveDown"/> (кнопка в кабине, зоны <see cref="LiftTrigger"/> у дверей).
///
/// Настройка в Unity (префабы в репозитории не трогаем — только чеклист для дизайнера):
/// - <b>Platform Rigidbody</b> — RB кабины (kinematic для DOMove).
/// - <b>Bottom / Top point</b> — мировые цели платформы.
/// - <b>Passenger seat points</b> — дочерние Transforms в кабине (слоты).
/// - <b>Boarding approach (bottom / top)</b> — точки на NavMesh у дверей; можно оставить пустыми и тогда берётся первый элемент <b>Bottom exit</b> / <b>Top exit</b>.
/// - <b>Top / Bottom exit points</b> — куда твиниться при высадке (после поездки и при выходе игрока из кабины без поездки).
/// - <b>Move duration</b>, <b>Seat board / Exit tween</b> — длительности.
/// - <b>Вход в кабину</b> — триггер-коллайдер Is Trigger на том же GameObject, что и этот компонент, либо на дочернем через <see cref="LiftCabinDetector"/>.
///   Учитывается только коллайдер игрока с тегом <b>Contact</b> (как у <see cref="PlayerContact"/>), обычно без своего Rigidbody. Слои Physics: триггер кабины и этот коллайдер пересекаются.
/// - После остановки на этаже, если игрок в кабине без пассажиров — посадка агентов с этажа (<see cref="TryBeginBoardingForAgents"/>). Если с игроком едут агенты — они выходят только после того, как игрок покинет кабину (можно уехать обратно).
/// - <b>Вызов с этажа</b> — коллайдеры с тегом <b>LiftTrigger</b> + <see cref="LiftTrigger"/> (низ/верх, <c>isUpTrigger</c>): игрок с тегом <b>Contact</b> через <see cref="PlayerContact"/>; только подъезд с другого этажа (<see cref="MoveDown"/> / <see cref="MoveUp"/>).
/// - <b>В кабине</b> — кнопка UI на <see cref="MoveToOppositeFloor"/> (на противоположный этаж). При необходимости отдельно <see cref="MoveUp"/> / <see cref="MoveDown"/>.
/// - Опционально <b>Cabin UI root</b> — как у <see cref="TraderNPC"/>: включается при входе Contact в кабину, выключается при выходе из объёма триггера.
/// </summary>
[RequireComponent(typeof(SaveId))]
public class Lift : MonoBehaviour
{
    [SerializeField, Tooltip("Кнопки/подсказки в кабине; включается при входе игрока (Contact), выключается при выходе из триггера.")]
    private GameObject _cabinUiRoot;

    [SerializeField] private Rigidbody platformRigidbody;
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private Transform topPoint;
    [SerializeField, Tooltip("Точка подхода на нижнем этаже. Если пусто — Bottom Exit Points[0].")]
    private Transform boardingApproachBottom;
    [SerializeField, Tooltip("Точка подхода на верхнем этаже. Если пусто — Top Exit Points[0].")]
    private Transform boardingApproachTop;
    [SerializeField] private Transform[] passengerSeatPoints;
    [SerializeField] private Transform[] topExitPoints;
    [SerializeField] private Transform[] bottomExitPoints;
    [SerializeField] private float moveDuration = 2f;

    [SerializeField] private float seatBoardTweenDuration = 0.45f;
    [SerializeField] private float exitTweenDuration = 0.4f;

    readonly List<Agent> _passengers = new();
    Tween _moveTween;
    bool _isMoving;
    bool _isAtTop;
    bool _playerInCabin;
    SaveId _saveId;

    /// <summary> True после начала MoveTo («ехать») в текущей сессии внутри кабины; сброс при новом входе игрока. </summary>
    bool _rideStartedThisCabinSession;

    /// <summary> Прибытие с игроком и пассажирами: высадка пассажиров отложена до выхода игрока из кабины. </summary>
    bool _pendingPassengerExitOnPlayerLeave;

    public string SaveId => GetSaveId().Id;

    void Awake()
    {
        GetSaveId();
        if (platformRigidbody == null)
        {
            Debug.LogError($"{nameof(Lift)}: Platform Rigidbody is not assigned.", this);
            enabled = false;
            return;
        }
        if (bottomPoint == null || topPoint == null)
        {
            Debug.LogError($"{nameof(Lift)}: Bottom/Top points are not assigned.", this);
            enabled = false;
            return;
        }

        _isAtTop = ComputeIsAtTopFromPlatformPosition();
    }

    void Start()
    {
        HideCabinUi();
    }

    public void MoveUp()
    {
        if (_isMoving) return;
        if (_isAtTop) return;

        MoveTo(topPoint, topExitPoints, true);
    }

    public void MoveDown()
    {
        if (_isMoving) return;
        if (!_isAtTop) return;

        MoveTo(bottomPoint, bottomExitPoints, false);
    }

    /// <summary> Одна кнопка «ехать»: с нижнего — наверх, с верхнего — вниз. </summary>
    public void MoveToOppositeFloor()
    {
        if (!_isAtTop)
            MoveUp();
        else
            MoveDown();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCabinPresenceCollider(other))
            return;

        NotifyPlayerEnteredCabin();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCabinPresenceCollider(other))
            return;

        NotifyPlayerLeftCabin();
    }

    static bool IsPlayerCabinPresenceCollider(Collider other) =>
        other != null && other.CompareTag("Contact");

    /// <summary> Игрок вошёл в объём триггера кабины. </summary>
    public void NotifyPlayerEnteredCabin()
    {
        _playerInCabin = true;
        _rideStartedThisCabinSession = false;
        TryBeginBoardingForAgents();
        ShowCabinUi();
    }

    /// <summary>
    /// Игрок вышел из кабины. Отложенная высадка пассажиров (если ждали выхода игрока) выполняется здесь.
    /// Если он не начинал поездку в этой «сессии», отмена подхода и высадка пассажиров на текущем этаже.
    /// При выходе во время или после того как нажали «ехать» (до остановки платформы) — не абортим: пассажиры следуют логике поездки/прибытия.
    /// </summary>
    public void NotifyPlayerLeftCabin()
    {
        _playerInCabin = false;
        HideCabinUi();

        if (_pendingPassengerExitOnPlayerLeave)
        {
            _pendingPassengerExitOnPlayerLeave = false;
            ExitPassengersTweenedAtCurrentFloor();
            TryBeginBoardingForAgents();
        }

        if (_rideStartedThisCabinSession)
            return;

        AbortCabinSessionWithoutRide();
    }

    void ShowCabinUi()
    {
        if (_cabinUiRoot == null)
            return;

        _cabinUiRoot.SetActive(true);
    }

    void HideCabinUi()
    {
        if (_cabinUiRoot == null)
            return;

        _cabinUiRoot.SetActive(false);
    }

    void TryBeginBoardingForAgents()
    {
        if (passengerSeatPoints == null || passengerSeatPoints.Length == 0)
            return;
        if (!TryGetBoardingApproachWorld(out Vector3 approachWorld))
            return;

        List<Agent> agents = SaveableRegistry.GetAll<Agent>();
        int seatIndex = 0;
        for (int i = 0; i < agents.Count && seatIndex < passengerSeatPoints.Length; i++)
        {
            Agent agent = agents[i];
            if (agent == null || !agent.isActiveAndEnabled)
                continue;
            if (agent.IsLiftPassenger)
                continue;
            if (agent.IsInCliffJump)
                continue;
            if (agent.IsBoardingThisLift(this))
                continue;

            agent.BeginLiftBoarding(
                this,
                approachWorld,
                passengerSeatPoints[seatIndex],
                seatBoardTweenDuration);
            seatIndex++;
        }
    }

    /// <summary>
    /// Подход: отдельные точки для этажа внизу и наверху; если не заданы — первый bottom/top exit.
    /// </summary>
    bool TryGetBoardingApproachWorld(out Vector3 position)
    {
        if (_isAtTop)
        {
            if (boardingApproachTop != null)
            {
                position = boardingApproachTop.position;
                return true;
            }

            if (topExitPoints != null && topExitPoints.Length > 0 && topExitPoints[0] != null)
            {
                position = topExitPoints[0].position;
                return true;
            }
        }
        else
        {
            if (boardingApproachBottom != null)
            {
                position = boardingApproachBottom.position;
                return true;
            }

            if (bottomExitPoints != null && bottomExitPoints.Length > 0 && bottomExitPoints[0] != null)
            {
                position = bottomExitPoints[0].position;
                return true;
            }
        }

        position = default;
        return false;
    }

    /// <summary> Точка выхода для текущего положения платформы (верхний / нижний этаж). </summary>
    Transform GetExitPointForCurrentFloor(int passengerSlotIndex)
    {
        Transform fallback = _isAtTop ? topPoint : bottomPoint;
        Transform[] exitPointsForCurrentFloor = _isAtTop ? topExitPoints : bottomExitPoints;
        return GetPoint(exitPointsForCurrentFloor, passengerSlotIndex, fallback);
    }

    void AbortCabinSessionWithoutRide()
    {
        _pendingPassengerExitOnPlayerLeave = false;

        var boardedCopy = new List<Agent>(_passengers);
        _passengers.Clear();

        float exitDur = Mathf.Max(0.05f, exitTweenDuration);
        for (int i = 0; i < boardedCopy.Count; i++)
        {
            Agent agent = boardedCopy[i];
            if (agent == null)
                continue;

            Transform exitPt = GetExitPointForCurrentFloor(i);
            agent.ForceExitLiftWithoutRide(this, exitPt, exitDur);
        }

        CancelPendingBoardingForThisLift();

        List<Agent> agents = SaveableRegistry.GetAll<Agent>();
        for (int i = 0; i < agents.Count; i++)
        {
            Agent agent = agents[i];
            if (agent != null && agent.IsRidingOnLift(this))
                agent.ForceExitLiftWithoutRide(this, GetExitPointForCurrentFloor(0), exitDur);
        }
    }

    void CancelPendingBoardingForThisLift()
    {
        List<Agent> agents = SaveableRegistry.GetAll<Agent>();
        for (int i = 0; i < agents.Count; i++)
        {
            Agent agent = agents[i];
            if (agent != null)
                agent.CancelLiftBoardingIfPendingFor(this);
        }
    }

    void MoveTo(Transform targetPoint, Transform[] exitPoints, bool isAtTop)
    {
        _rideStartedThisCabinSession = true;
        NotifyLiftDeparting();
        HideCabinUi();

        _isMoving = true;
        _moveTween?.Kill();
        _moveTween = platformRigidbody.DOMove(targetPoint.position, moveDuration)
            .SetEase(Ease.InOutQuad)
            .SetUpdate(UpdateType.Fixed)
            .OnComplete(() =>
            {
                _isMoving = false;
                _isAtTop = isAtTop;

                bool deferPassengerExit = _playerInCabin && _passengers.Count > 0;
                if (deferPassengerExit)
                    _pendingPassengerExitOnPlayerLeave = true;
                else
                    ExitPassengersTweened(exitPoints, targetPoint);

                if (_playerInCabin)
                {
                    if (!deferPassengerExit)
                        TryBeginBoardingForAgents();
                    ShowCabinUi();
                }
            });
    }

    /// <summary> В начале движения платформы: отменить подход; mid-tween — перейти в local-твин к сиденью. </summary>
    void NotifyLiftDeparting()
    {
        List<Agent> agents = SaveableRegistry.GetAll<Agent>();
        for (int i = 0; i < agents.Count; i++)
        {
            Agent agent = agents[i];
            if (agent != null)
                agent.NotifyLiftDeparting(this);
        }
    }

    public void RegisterPassenger(Agent agent)
    {
        if (agent == null || _passengers.Contains(agent))
            return;
        _passengers.Add(agent);
    }

    void ExitPassengersTweened(Transform[] exitPoints, Transform fallbackPoint)
    {
        for (int i = _passengers.Count - 1; i >= 0; i--)
        {
            Agent agent = _passengers[i];
            if (agent == null)
            {
                _passengers.RemoveAt(i);
                continue;
            }

            Transform exitPoint = GetPoint(exitPoints, i, fallbackPoint);
            agent.BeginLiftExit(this, exitPoint, exitTweenDuration);
        }

        _passengers.Clear();
    }

    void ExitPassengersTweenedAtCurrentFloor()
    {
        Transform fallback = _isAtTop ? topPoint : bottomPoint;
        Transform[] exitPoints = _isAtTop ? topExitPoints : bottomExitPoints;
        ExitPassengersTweened(exitPoints, fallback);
    }

    public bool IsMoving() => _isMoving;

    public bool IsAtTop() => _isAtTop;

    public LiftSaveData CaptureSaveData()
    {
        return new LiftSaveData
        {
            id = SaveId,
            isAtTop = ComputeIsAtTopFromPlatformPosition(),
        };
    }

    public void RestoreSaveData(LiftSaveData data)
    {
        if (data == null || platformRigidbody == null || bottomPoint == null || topPoint == null) return;

        _moveTween?.Kill();
        _moveTween = null;
        _isMoving = false;
        _passengers.Clear();
        _playerInCabin = false;
        _pendingPassengerExitOnPlayerLeave = false;

        Transform targetPoint = data.isAtTop ? topPoint : bottomPoint;
        platformRigidbody.position = targetPoint.position;
        platformRigidbody.rotation = targetPoint.rotation;
        _isAtTop = data.isAtTop;
        Physics.SyncTransforms();
    }

    bool ComputeIsAtTopFromPlatformPosition()
    {
        if (platformRigidbody == null || bottomPoint == null || topPoint == null) return false;

        float toTop = Vector3.SqrMagnitude(platformRigidbody.position - topPoint.position);
        float toBottom = Vector3.SqrMagnitude(platformRigidbody.position - bottomPoint.position);
        return toTop < toBottom;
    }

    Transform GetPoint(Transform[] points, int index, Transform fallbackPoint)
    {
        if (points != null && points.Length > 0)
            return points[Mathf.Min(index, points.Length - 1)];

        return fallbackPoint;
    }

    SaveId GetSaveId()
    {
        if (_saveId == null && !TryGetComponent(out _saveId))
            _saveId = gameObject.AddComponent<SaveId>();

        return _saveId;
    }
}
