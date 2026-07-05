using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CityDef.Gameplay.Logic.Tests
{
    public sealed class LootScatterTests
    {
        // ---- BuildAnglePool ----

        [Test]
        public void BuildAnglePool_NothingBlocked_ReturnsAllSlots()
        {
            var pool = LootScatter.BuildAnglePool(new HashSet<int>(), new HashSet<int>(), LootScatter.AngleSlots);

            Assert.AreEqual(LootScatter.AngleSlots, pool.Count);
        }

        [Test]
        public void BuildAnglePool_ExcludesBlockedAndSucceeded()
        {
            var blocked = new HashSet<int> { 0, 1 };
            var succeeded = new HashSet<int> { 2 };

            var pool = LootScatter.BuildAnglePool(blocked, succeeded, LootScatter.AngleSlots);

            Assert.AreEqual(LootScatter.AngleSlots - 3, pool.Count);
            CollectionAssert.DoesNotContain(pool, 0);
            CollectionAssert.DoesNotContain(pool, 1);
            CollectionAssert.DoesNotContain(pool, 2);
        }

        [Test]
        public void BuildAnglePool_AllBlocked_ReturnsEmpty()
        {
            var blocked = new HashSet<int>();
            for (int i = 0; i < LootScatter.AngleSlots; i++)
                blocked.Add(i);

            var pool = LootScatter.BuildAnglePool(blocked, new HashSet<int>(), LootScatter.AngleSlots);

            Assert.IsEmpty(pool);
        }

        [Test]
        public void BuildAnglePool_NullSets_ReturnsAllSlots()
        {
            var pool = LootScatter.BuildAnglePool(null, null, LootScatter.AngleSlots);

            Assert.AreEqual(LootScatter.AngleSlots, pool.Count);
        }

        // ---- CandidateAtAngle ----

        [Test]
        public void CandidateAtAngle_LandsAtScatterDistanceOnXZ()
        {
            var origin = new Vector3(5f, 2f, -3f);
            const float distance = 4f;

            for (int slot = 0; slot < LootScatter.AngleSlots; slot++)
            {
                Vector3 p = LootScatter.CandidateAtAngle(origin, distance, slot, LootScatter.AngleSlots);
                float horiz = new Vector2(p.x - origin.x, p.z - origin.z).magnitude;
                Assert.AreEqual(distance, horiz, 1e-3f, $"slot {slot} off the scatter circle");
            }
        }

        [Test]
        public void CandidateAtAngle_Slot0_PointsAlongForward()
        {
            Vector3 p = LootScatter.CandidateAtAngle(Vector3.zero, 10f, 0, LootScatter.AngleSlots);

            Assert.AreEqual(0f, p.x, 1e-3f);
            Assert.AreEqual(10f, p.z, 1e-3f);
        }

        // ---- IsWithinScatter ----

        [Test]
        public void IsWithinScatter_WithinToleranceOnBoundary_True()
        {
            var origin = Vector3.zero;
            var point = new Vector3(0f, 100f, 5.4f); // высота игнорируется

            Assert.IsTrue(LootScatter.IsWithinScatter(origin, point, 5f, 0.5f));
        }

        [Test]
        public void IsWithinScatter_BeyondTolerance_False()
        {
            var origin = Vector3.zero;
            var point = new Vector3(0f, 0f, 5.6f);

            Assert.IsFalse(LootScatter.IsWithinScatter(origin, point, 5f, 0.5f));
        }

        // ---- IsFarFromPlaced ----

        [Test]
        public void IsFarFromPlaced_EmptyList_True()
        {
            Assert.IsTrue(LootScatter.IsFarFromPlaced(Vector3.zero, new List<Vector3>(), 1f));
        }

        [Test]
        public void IsFarFromPlaced_TooCloseOnXZ_False()
        {
            var placed = new List<Vector3> { new Vector3(0.4f, 99f, 0f) }; // высота не влияет

            Assert.IsFalse(LootScatter.IsFarFromPlaced(Vector3.zero, placed, 0.5f));
        }

        [Test]
        public void IsFarFromPlaced_BeyondSeparation_True()
        {
            var placed = new List<Vector3> { new Vector3(0.6f, 0f, 0f) };

            Assert.IsTrue(LootScatter.IsFarFromPlaced(Vector3.zero, placed, 0.5f));
        }

        [Test]
        public void IsFarFromPlaced_ZeroSeparation_AlwaysTrue()
        {
            var placed = new List<Vector3> { Vector3.zero };

            Assert.IsTrue(LootScatter.IsFarFromPlaced(Vector3.zero, placed, 0f));
        }
    }
}
