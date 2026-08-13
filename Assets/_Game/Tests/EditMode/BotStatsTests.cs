using System.Collections.Generic;
using NUnit.Framework;

namespace CityDef.Gameplay.Logic.Tests
{
    public sealed class BotStatsTests
    {
        static BotStats MakeLaborerLike()
        {
            return new BotStats(new[]
            {
                new BotStatDefinition { type = BotStatType.Mining, baseValue = 25f, valuePerLevel = 5f, maxLevel = 10 },
                new BotStatDefinition { type = BotStatType.MiningSpeed, baseValue = 1f, valuePerLevel = 0.1f, maxLevel = 10 },
            });
        }

        [Test]
        public void GetValue_Level1_ReturnsBase()
        {
            Assert.AreEqual(25f, MakeLaborerLike().GetValue(BotStatType.Mining));
        }

        [Test]
        public void GetValue_GrowsPerLevel()
        {
            BotStats stats = MakeLaborerLike();
            stats.TryLevelUp(BotStatType.Mining);
            stats.TryLevelUp(BotStatType.Mining);

            Assert.AreEqual(3, stats.GetLevel(BotStatType.Mining));
            Assert.AreEqual(35f, stats.GetValue(BotStatType.Mining));
        }

        [Test]
        public void TryLevelUp_StopsAtMaxLevel()
        {
            var stats = new BotStats(new[]
            {
                new BotStatDefinition { type = BotStatType.Energy, baseValue = 100f, valuePerLevel = 10f, maxLevel = 2 },
            });

            Assert.IsTrue(stats.TryLevelUp(BotStatType.Energy));
            Assert.IsFalse(stats.TryLevelUp(BotStatType.Energy));
            Assert.AreEqual(2, stats.GetLevel(BotStatType.Energy));
            Assert.IsFalse(stats.CanLevelUp(BotStatType.Energy));
        }

        [Test]
        public void GetValue_UndefinedStat_ReturnsZeroAndLevelCapIsOne()
        {
            BotStats stats = MakeLaborerLike();

            Assert.AreEqual(0f, stats.GetValue(BotStatType.HealthRegen));
            Assert.IsFalse(stats.CanLevelUp(BotStatType.HealthRegen));
        }

        [Test]
        public void CaptureRestore_RoundTripsLevels()
        {
            BotStats source = MakeLaborerLike();
            source.TryLevelUp(BotStatType.Mining);
            source.TryLevelUp(BotStatType.MiningSpeed);
            source.TryLevelUp(BotStatType.MiningSpeed);

            List<BotStatLevelSaveData> saved = source.CaptureSaveData();
            BotStats restored = MakeLaborerLike();
            restored.RestoreSaveData(saved);

            Assert.AreEqual(2, restored.GetLevel(BotStatType.Mining));
            Assert.AreEqual(3, restored.GetLevel(BotStatType.MiningSpeed));
        }

        [Test]
        public void RestoreSaveData_ClampsToMaxLevelAndSkipsUnknownIds()
        {
            var stats = new BotStats(new[]
            {
                new BotStatDefinition { type = BotStatType.Mining, baseValue = 25f, valuePerLevel = 5f, maxLevel = 3 },
            });

            stats.RestoreSaveData(new List<BotStatLevelSaveData>
            {
                new BotStatLevelSaveData { statId = "Mining", level = 99 },
                new BotStatLevelSaveData { statId = "NoSuchStat", level = 5 },
            });

            Assert.AreEqual(3, stats.GetLevel(BotStatType.Mining));
        }
    }
}
