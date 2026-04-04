using System;
using ColorChargeTD.Domain;
using UnityEngine;

namespace ColorChargeTD.Data
{
    [Serializable]
    public struct LevelUnlockRule
    {
        [SerializeField] private bool unlockedByDefault;
        [SerializeField] private string requiredLevelId;

        public bool UnlockedByDefault => unlockedByDefault;
        public string RequiredLevelId => requiredLevelId;

        public static LevelUnlockRule CreateFirstLevel()
        {
            LevelUnlockRule rule = new LevelUnlockRule();
            rule.unlockedByDefault = true;
            rule.requiredLevelId = string.Empty;
            return rule;
        }
    }

    [Serializable]
    public struct LevelRewardDefinition
    {
        public static readonly LevelRewardDefinition Default = new LevelRewardDefinition
        {
            SoftCurrency = 25,
            FirstCompletionBonus = 10,
        };

        [SerializeField] private int softCurrency;
        [SerializeField] private int firstCompletionBonus;

        public int SoftCurrency
        {
            get => softCurrency;
            set => softCurrency = value;
        }

        public int FirstCompletionBonus
        {
            get => firstCompletionBonus;
            set => firstCompletionBonus = value;
        }
    }

    [Serializable]
    public struct WaveSpawnGroup
    {
        [SerializeField] private EnemyDefinition enemy;
        [SerializeField] private string pathId;
        [SerializeField] private int count;
        [SerializeField] private float startDelay;
        [SerializeField] private float spawnInterval;

        public EnemyDefinition Enemy => enemy;
        public string PathId => pathId;
        public int Count => count;
        public float StartDelay => Mathf.Max(0f, startDelay);
        public float SpawnInterval => Mathf.Max(0f, spawnInterval);
    }

    [Serializable]
    public struct ContentValidationMessage
    {
        [SerializeField] private ValidationSeverity severity;
        [SerializeField] private string source;
        [SerializeField] private string message;

        public ValidationSeverity Severity => severity;
        public string Source => source;
        public string Message => message;

        public static ContentValidationMessage Info(string source, string message)
        {
            return new ContentValidationMessage
            {
                severity = ValidationSeverity.Info,
                source = source,
                message = message,
            };
        }

        public static ContentValidationMessage Warning(string source, string message)
        {
            return new ContentValidationMessage
            {
                severity = ValidationSeverity.Warning,
                source = source,
                message = message,
            };
        }

        public static ContentValidationMessage Error(string source, string message)
        {
            return new ContentValidationMessage
            {
                severity = ValidationSeverity.Error,
                source = source,
                message = message,
            };
        }
    }

    [Serializable]
    public struct LevelLayoutRuntimeDefinition
    {
        [SerializeField] private string layoutId;
        [SerializeField] private LevelPathRuntimeDefinition[] paths;
        [SerializeField] private BuildSlotRuntimeDefinition[] slots;
        [SerializeField] private Vector3 basePosition;

        public string LayoutId => layoutId;
        public LevelPathRuntimeDefinition[] Paths => paths;
        public BuildSlotRuntimeDefinition[] Slots => slots;
        public Vector3 BasePosition => basePosition;

        public LevelLayoutRuntimeDefinition(string layoutId, LevelPathRuntimeDefinition[] paths, BuildSlotRuntimeDefinition[] slots, Vector3 basePosition)
        {
            this.layoutId = layoutId;
            this.paths = paths;
            this.slots = slots;
            this.basePosition = basePosition;
        }
    }

    [Serializable]
    public struct LevelPathRuntimeDefinition
    {
        [SerializeField] private string pathId;
        [SerializeField] private EnemyMovePattern movePattern;
        [SerializeField] private Vector3[] waypoints;

        public string PathId => pathId;
        public EnemyMovePattern MovePattern => movePattern;
        public Vector3[] Waypoints => waypoints;

        public LevelPathRuntimeDefinition(string pathId, EnemyMovePattern movePattern, Vector3[] waypoints)
        {
            this.pathId = pathId;
            this.movePattern = movePattern;
            this.waypoints = waypoints;
        }
    }

    [Serializable]
    public struct BuildSlotRuntimeDefinition
    {
        [SerializeField] private string slotId;
        [SerializeField] private Vector3 position;
        [SerializeField] private float radius;

        public string SlotId => slotId;
        public Vector3 Position => position;
        public float Radius => radius;

        public BuildSlotRuntimeDefinition(string slotId, Vector3 position, float radius)
        {
            this.slotId = slotId;
            this.position = position;
            this.radius = radius;
        }
    }
}
