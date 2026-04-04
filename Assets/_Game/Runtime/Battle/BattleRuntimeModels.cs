using System;
using System.Collections.Generic;
using ColorChargeTD.Data;
using ColorChargeTD.Domain;
using UnityEngine;
using UnityEngine.Events;

namespace ColorChargeTD.Battle
{
    public sealed class LevelSessionRuntime
    {
        private readonly HashSet<string> occupiedSlots = new HashSet<string>(StringComparer.Ordinal);

        public LevelSessionRuntime(
            LevelDefinition levelDefinition,
            LevelLayoutRuntimeDefinition layoutDefinition,
            GameBalanceConfig balanceConfig,
            float startingChargeNormalized,
            int startingResource)
        {
            LevelDefinition = levelDefinition;
            LayoutDefinition = layoutDefinition;
            StartingChargeNormalized = startingChargeNormalized;
            RemainingLives = balanceConfig != null ? balanceConfig.BaseLives : 10;
            CurrentResource = startingResource;
        }

        public LevelDefinition LevelDefinition { get; }
        public LevelLayoutRuntimeDefinition LayoutDefinition { get; }
        public float StartingChargeNormalized { get; }
        public int CurrentResource { get; set; }
        public int RemainingLives { get; private set; }
        public int AccumulatedKillReward { get; set; }
        public bool IsFinished { get; private set; }
        public BattleOutcome Outcome { get; private set; }

        public int TotalBuildSlotCount =>
            LayoutDefinition.Slots != null ? LayoutDefinition.Slots.Length : 0;

        public int OccupiedBuildSlotCount => occupiedSlots.Count;

        public BuildSlotRuntimeDefinition FindSlot(string slotId)
        {
            BuildSlotRuntimeDefinition[] slots = LayoutDefinition.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                if (string.Equals(slots[i].SlotId, slotId, StringComparison.Ordinal))
                {
                    return slots[i];
                }
            }

            return default;
        }

        public bool IsSlotOccupied(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                return true;
            }

            return occupiedSlots.Contains(slotId);
        }

        public void OccupySlot(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                return;
            }

            occupiedSlots.Add(slotId);
        }

        public void LoseLife()
        {
            RemainingLives = Mathf.Max(0, RemainingLives - 1);
        }

        public void Finish(BattleOutcome outcome)
        {
            Outcome = outcome;
            IsFinished = true;
        }
    }

    public sealed class EnemyRuntimeModel
    {
        public EnemyRuntimeModel(EnemyDefinition definition, LevelPathRuntimeDefinition path)
        {
            Definition = definition;
            Path = path;
            CurrentHitPoints = definition != null ? definition.HitPoints : 1;
            Position = path.Waypoints != null && path.Waypoints.Length > 0 ? path.Waypoints[0] : Vector3.zero;
        }

        public EnemyDefinition Definition { get; }
        public LevelPathRuntimeDefinition Path { get; }
        public int CurrentHitPoints { get; private set; }
        public int WaypointIndex { get; set; }
        public float DistanceToNextWaypoint { get; set; }
        public Vector3 Position { get; set; }
        public bool ReachedGoal { get; set; }

        public bool IsAlive => CurrentHitPoints > 0 && !ReachedGoal;

        public void ApplyDamage(int amount)
        {
            CurrentHitPoints = Mathf.Max(0, CurrentHitPoints - Mathf.Max(0, amount));
        }
    }

    public sealed class TowerRuntimeModel
    {
        public TowerRuntimeModel(TowerDefinition definition, BuildSlotRuntimeDefinition slot, float startingChargeNormalized)
        {
            Definition = definition;
            Slot = slot;
            Charge = definition.Capacity * Mathf.Clamp01(startingChargeNormalized);
        }

        public TowerDefinition Definition { get; }
        public BuildSlotRuntimeDefinition Slot { get; }
        public float Charge { get; set; }
        public float FireCooldown { get; set; }
        public EnemyRuntimeModel CurrentAimTarget { get; set; }

        public bool HasCharge => Charge >= 1f;
        public bool IsFull => Charge >= Definition.Capacity;
    }

    public sealed class WaveSpawnerSystem
    {
        private readonly WaveDefinition waveDefinition;
        private readonly Dictionary<string, LevelPathRuntimeDefinition> pathsById = new Dictionary<string, LevelPathRuntimeDefinition>(StringComparer.Ordinal);

        private int groupIndex;
        private float elapsedInGroup;
        private int spawnedInGroup;
        private float totalElapsed;
        private bool awaitingPlayerAck = true;

        public WaveSpawnerSystem(WaveDefinition waveDefinition, LevelLayoutRuntimeDefinition layoutDefinition)
        {
            this.waveDefinition = waveDefinition;

            LevelPathRuntimeDefinition[] paths = layoutDefinition.Paths;
            for (int i = 0; i < paths.Length; i++)
            {
                LevelPathRuntimeDefinition path = paths[i];
                if (!string.IsNullOrWhiteSpace(path.PathId))
                {
                    pathsById[path.PathId] = path;
                }
            }
        }

        public bool NeedsPlayerStart => awaitingPlayerAck && !IsComplete;

        public void AcknowledgeStartWave()
        {
            if (IsComplete)
            {
                return;
            }

            awaitingPlayerAck = false;
        }

        public bool IsComplete => waveDefinition == null || groupIndex >= waveDefinition.Groups.Count;

        public int TotalWaveGroups =>
            waveDefinition != null && waveDefinition.Groups != null ? waveDefinition.Groups.Count : 0;

        public int DisplayWaveNumber
        {
            get
            {
                int total = TotalWaveGroups;
                if (total <= 0)
                {
                    return 0;
                }

                if (IsComplete)
                {
                    return total;
                }

                return Mathf.Clamp(groupIndex + 1, 1, total);
            }
        }

        public float ProgressNormalized
        {
            get
            {
                if (waveDefinition == null)
                {
                    return 1f;
                }

                float duration = Mathf.Max(0.01f, waveDefinition.GetTotalDuration());
                return Mathf.Clamp01(totalElapsed / duration);
            }
        }

        public void Tick(float deltaTime, List<EnemyRuntimeModel> enemies)
        {
            if (waveDefinition == null || IsComplete)
            {
                return;
            }

            if (awaitingPlayerAck)
            {
                return;
            }

            totalElapsed += deltaTime;
            elapsedInGroup += deltaTime;

            WaveSpawnGroup group = waveDefinition.Groups[groupIndex];
            if (elapsedInGroup < group.StartDelay)
            {
                return;
            }

            while (spawnedInGroup < group.Count && elapsedInGroup >= group.StartDelay + group.SpawnInterval * spawnedInGroup)
            {
                LevelPathRuntimeDefinition path = ResolvePath(group);
                enemies.Add(new EnemyRuntimeModel(group.Enemy, path));
                spawnedInGroup++;
            }

            if (spawnedInGroup < group.Count)
            {
                return;
            }

            groupIndex++;
            spawnedInGroup = 0;
            elapsedInGroup = 0f;

            if (groupIndex < waveDefinition.Groups.Count)
            {
                awaitingPlayerAck = true;
            }
        }

        private LevelPathRuntimeDefinition ResolvePath(WaveSpawnGroup group)
        {
            if (!string.IsNullOrWhiteSpace(group.PathId) && pathsById.TryGetValue(group.PathId, out LevelPathRuntimeDefinition path))
            {
                return path;
            }

            foreach (KeyValuePair<string, LevelPathRuntimeDefinition> pair in pathsById)
            {
                return pair.Value;
            }

            return default;
        }
    }

    public sealed class EnemyPathSystem
    {
        public void Tick(float deltaTime, List<EnemyRuntimeModel> enemies, LevelSessionRuntime session)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyRuntimeModel enemy = enemies[i];
                if (!enemy.IsAlive)
                {
                    if (enemy.CurrentHitPoints <= 0 && enemy.Definition != null)
                    {
                        session.AccumulatedKillReward += enemy.Definition.BaseReward;
                    }

                    enemies.RemoveAt(i);
                    continue;
                }

                MoveEnemy(deltaTime, enemy, session);
            }
        }

        private void MoveEnemy(float deltaTime, EnemyRuntimeModel enemy, LevelSessionRuntime session)
        {
            Vector3[] waypoints = enemy.Path.Waypoints;
            if (waypoints == null || waypoints.Length == 0)
            {
                enemy.ReachedGoal = true;
                session.LoseLife();
                return;
            }

            int nextIndex = Mathf.Min(enemy.WaypointIndex + 1, waypoints.Length - 1);
            Vector3 target = waypoints[nextIndex];
            float speed = enemy.Definition != null ? enemy.Definition.Speed : 1f;

            enemy.Position = Vector3.MoveTowards(enemy.Position, target, speed * deltaTime);
            if ((enemy.Position - target).sqrMagnitude > 0.0001f)
            {
                return;
            }

            enemy.WaypointIndex = nextIndex;
            if (enemy.WaypointIndex < waypoints.Length - 1)
            {
                return;
            }

            enemy.ReachedGoal = true;
            session.LoseLife();
        }
    }

    public sealed class TowerChargeSystem
    {
        public void Tick(float deltaTime, List<TowerRuntimeModel> towers)
        {
            for (int i = 0; i < towers.Count; i++)
            {
                TowerRuntimeModel tower = towers[i];
                tower.Charge = Mathf.Min(tower.Definition.Capacity, tower.Charge + tower.Definition.ProductionPerSecond * deltaTime);
                tower.FireCooldown = Mathf.Max(0f, tower.FireCooldown - deltaTime);
            }
        }
    }

    public sealed class TowerTargetingSystem
    {
        public void Tick(
            List<TowerRuntimeModel> towers,
            List<EnemyRuntimeModel> enemies,
            DamageResolver damageResolver,
            ProjectileHitScheduler projectileHits,
            UnityAction<TowerRuntimeModel, EnemyRuntimeModel> onTowerFired)
        {
            for (int i = 0; i < towers.Count; i++)
            {
                TowerRuntimeModel tower = towers[i];
                tower.CurrentAimTarget = AcquireTarget(tower, enemies);
            }

            for (int i = 0; i < towers.Count; i++)
            {
                TowerRuntimeModel tower = towers[i];
                if (!tower.HasCharge || tower.FireCooldown > 0f)
                {
                    continue;
                }

                EnemyRuntimeModel target = tower.CurrentAimTarget;
                if (target == null)
                {
                    continue;
                }

                int damage = damageResolver.ComputeShotDamage(tower);
                float travelTime = tower.Definition.ProjectileTravelTime;
                projectileHits.Enqueue(target, travelTime, damage, tower.Definition.Color);
                tower.Charge = Mathf.Max(0f, tower.Charge - 1f);
                tower.FireCooldown = 1f / tower.Definition.FireRatePerSecond;
                onTowerFired?.Invoke(tower, target);
            }
        }

        private EnemyRuntimeModel AcquireTarget(TowerRuntimeModel tower, List<EnemyRuntimeModel> enemies)
        {
            float maxRangeSquared = tower.Definition.Range * tower.Definition.Range;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyRuntimeModel enemy = enemies[i];
                if (!enemy.IsAlive || enemy.Definition == null || enemy.Definition.Color != tower.Definition.Color)
                {
                    continue;
                }

                float distanceSquared = (enemy.Position - tower.Slot.Position).sqrMagnitude;
                if (distanceSquared <= maxRangeSquared)
                {
                    return enemy;
                }
            }

            return null;
        }
    }

    public sealed class DamageResolver
    {
        private readonly GameBalanceConfig balanceConfig;

        public DamageResolver(GameBalanceConfig balanceConfig)
        {
            this.balanceConfig = balanceConfig;
        }

        public int ComputeShotDamage(TowerRuntimeModel tower)
        {
            if (tower == null || tower.Definition == null)
            {
                return 0;
            }

            int damage = tower.Definition.DamagePerShot;
            if (balanceConfig != null && balanceConfig.EnableOvercharge && tower.IsFull)
            {
                damage = Mathf.RoundToInt(damage * tower.Definition.OverchargeMultiplier);
            }

            return damage;
        }

        public void ApplyDelayedDamage(EnemyRuntimeModel enemy, int damage, ColorCharge towerColor)
        {
            if (enemy == null || !enemy.IsAlive || enemy.Definition == null)
            {
                return;
            }

            if (enemy.Definition.Color != towerColor)
            {
                return;
            }

            enemy.ApplyDamage(damage);
        }

        public void ResolveShot(TowerRuntimeModel tower, EnemyRuntimeModel enemy)
        {
            if (tower == null || enemy == null || tower.Definition == null || enemy.Definition == null)
            {
                return;
            }

            if (tower.Definition.Color != enemy.Definition.Color)
            {
                return;
            }

            int damage = ComputeShotDamage(tower);
            enemy.ApplyDamage(damage);
        }
    }

    public sealed class BattleResultService
    {
        public BattleOutcome Evaluate(LevelSessionRuntime session, WaveSpawnerSystem waveSpawnerSystem, List<EnemyRuntimeModel> enemies)
        {
            if (session.RemainingLives <= 0)
            {
                return BattleOutcome.Defeat;
            }

            bool noEnemiesLeft = enemies.Count == 0;
            if (waveSpawnerSystem != null && waveSpawnerSystem.IsComplete && noEnemiesLeft)
            {
                return BattleOutcome.Victory;
            }

            return BattleOutcome.None;
        }
    }
}
