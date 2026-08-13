using System;
using System.Collections.Generic;

namespace CityDef.Gameplay.Logic
{
    /// <summary>
    /// Описание одной характеристики в специализации: база, рост за уровень, потолок.
    /// Встраивается в конфиг специализации (ScriptableObject) как сериализуемые данные.
    /// </summary>
    [Serializable]
    public class BotStatDefinition
    {
        public BotStatType type;
        public float baseValue = 1f;
        public float valuePerLevel;
        public int maxLevel = 10;

        /// <summary> Значение характеристики на уровне (уровни считаются с 1). </summary>
        public float GetValue(int level)
        {
            int clampedLevel = Math.Clamp(level, 1, Math.Max(1, maxLevel));
            return baseValue + valuePerLevel * (clampedLevel - 1);
        }
    }

    /// <summary> Уровень одной характеристики в сейве бота. </summary>
    [Serializable]
    public class BotStatLevelSaveData
    {
        public string statId;
        public int level;
    }

    /// <summary>
    /// Уровни характеристик одного бота поверх описаний специализации.
    /// Чистая логика: уровень каждой характеристики 1..maxLevel, значение считает
    /// <see cref="BotStatDefinition"/>. Тестируется в EditMode без сцены.
    /// </summary>
    public class BotStats
    {
        public static readonly BotStatType[] AllTypes = (BotStatType[])Enum.GetValues(typeof(BotStatType));

        private readonly Dictionary<BotStatType, BotStatDefinition> _definitions =
            new Dictionary<BotStatType, BotStatDefinition>();
        private readonly Dictionary<BotStatType, int> _levels = new Dictionary<BotStatType, int>();

        public BotStats(IEnumerable<BotStatDefinition> definitions)
        {
            if (definitions == null) return;

            foreach (BotStatDefinition definition in definitions)
            {
                if (definition != null)
                    _definitions[definition.type] = definition;
            }
        }

        public int GetLevel(BotStatType type)
        {
            return _levels.TryGetValue(type, out int level) ? level : 1;
        }

        public int GetMaxLevel(BotStatType type)
        {
            return _definitions.TryGetValue(type, out BotStatDefinition definition)
                ? Math.Max(1, definition.maxLevel)
                : 1;
        }

        public float GetValue(BotStatType type)
        {
            return _definitions.TryGetValue(type, out BotStatDefinition definition)
                ? definition.GetValue(GetLevel(type))
                : 0f;
        }

        public bool CanLevelUp(BotStatType type)
        {
            return GetLevel(type) < GetMaxLevel(type);
        }

        /// <summary> Поднять уровень характеристики на 1 (прокачка). false — упёрлись в потолок. </summary>
        public bool TryLevelUp(BotStatType type)
        {
            if (!CanLevelUp(type)) return false;

            _levels[type] = GetLevel(type) + 1;
            return true;
        }

        public List<BotStatLevelSaveData> CaptureSaveData()
        {
            var data = new List<BotStatLevelSaveData>(AllTypes.Length);
            for (int i = 0; i < AllTypes.Length; i++)
            {
                data.Add(new BotStatLevelSaveData
                {
                    statId = AllTypes[i].ToString(),
                    level = GetLevel(AllTypes[i]),
                });
            }

            return data;
        }

        public void RestoreSaveData(List<BotStatLevelSaveData> data)
        {
            _levels.Clear();
            if (data == null) return;

            for (int i = 0; i < data.Count; i++)
            {
                BotStatLevelSaveData entry = data[i];
                if (entry == null || !Enum.TryParse(entry.statId, out BotStatType type))
                    continue;

                _levels[type] = Math.Clamp(entry.level, 1, GetMaxLevel(type));
            }
        }
    }
}
