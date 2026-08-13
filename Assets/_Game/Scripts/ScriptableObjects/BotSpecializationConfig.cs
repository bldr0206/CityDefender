using System.Collections.Generic;
using CityDef.Gameplay.Logic;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// Специализация бота: имя для UI и набор характеристик с базой, ростом за уровень
/// и потолком. Конфиг задаёт «что качается и почём»; текущие уровни конкретного
/// бота живут в <see cref="BotStats"/>.
/// </summary>
[CreateAssetMenu(fileName = "BotSpecialization", menuName = "Scriptable Objects/BotSpecializationConfig")]
public class BotSpecializationConfig : ScriptableObject
{
    [SerializeField] private string _id = "laborer";
    [SerializeField] private LocalizedString _displayName;
    [SerializeField] private BotStatDefinition[] _stats;

    public string Id => _id;
    public LocalizedString DisplayName => _displayName;
    public IReadOnlyList<BotStatDefinition> Stats => _stats;
}
