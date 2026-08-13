using System.Collections.Generic;
using UnityEngine;
using Zenject;

/// <summary>
/// Триггер на сцене (бокс-коллайдер Is Trigger): при входе игрока проигрывает последовательность
/// катсцена/диалог/пауза — тот же <see cref="QuestSequenceStep"/> и редактор, что у квестов.
/// One-shot срабатывает один раз и сохраняется как «использованный»; иначе — при каждом входе.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(SaveId))]
public class SequenceTrigger : MonoBehaviour
{
    [SerializeField] List<QuestSequenceStep> _sequence = new List<QuestSequenceStep>();
    [Tooltip("Сработать один раз и деактивироваться. Иначе — каждый раз при входе игрока.")]
    [SerializeField] bool _oneShot = true;

    QuestSequencePlayer _player;
    SaveId _saveId;
    bool _consumed;
    bool _isPlaying;

    public string SaveId => GetSaveId().Id;

    [Inject]
    public void Construct(DialogueScreen dialogueScreen)
    {
        _player = new QuestSequencePlayer(this, dialogueScreen);
    }

    void Awake()
    {
        GetSaveId();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(GameTags.Contact)) return;
        if (_consumed || _isPlaying) return;

        _isPlaying = true;
        _player.Play(_sequence, OnSequenceFinished);
    }

    void OnSequenceFinished()
    {
        _isPlaying = false;
        if (_oneShot)
            _consumed = true;
    }

    public SequenceTriggerSaveData CaptureSaveData()
    {
        return new SequenceTriggerSaveData
        {
            id = SaveId,
            consumed = _consumed,
        };
    }

    public void RestoreSaveData(SequenceTriggerSaveData data)
    {
        _consumed = data.consumed;
    }

    SaveId GetSaveId()
    {
        if (_saveId == null && !TryGetComponent(out _saveId))
            _saveId = gameObject.AddComponent<SaveId>();

        return _saveId;
    }
}
