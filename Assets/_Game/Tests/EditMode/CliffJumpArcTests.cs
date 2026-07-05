using NUnit.Framework;
using UnityEngine;

namespace CityDef.Gameplay.Logic.Tests
{
    public sealed class CliffJumpArcTests
    {
        static readonly Vector3 Start = new Vector3(0f, 0f, 0f);
        static readonly Vector3 End = new Vector3(10f, 0f, 0f);

        [Test]
        public void BuildWaypoints_SingleJump_PeakThenEnd()
        {
            Vector3[] path = CliffJumpArc.BuildWaypoints(Start, End, 3f, 1);

            Assert.AreEqual(2, path.Length);
            // вершина на середине хорды, поднята на jumpPower
            Assert.AreEqual(new Vector3(5f, 3f, 0f), path[0]);
            Assert.AreEqual(End, path[1]);
        }

        [Test]
        public void BuildWaypoints_TwoJumps_PeakValleyPeakEnd()
        {
            Vector3[] path = CliffJumpArc.BuildWaypoints(Start, End, 2f, 2);

            Assert.AreEqual(4, path.Length);
            Assert.AreEqual(new Vector3(2.5f, 2f, 0f), path[0]); // вершина 1-го горба
            Assert.AreEqual(new Vector3(5f, 0f, 0f), path[1]);   // впадина на хорде
            Assert.AreEqual(new Vector3(7.5f, 2f, 0f), path[2]); // вершина 2-го горба
            Assert.AreEqual(End, path[3]);
        }

        [Test]
        public void BuildWaypoints_LastPointIsAlwaysEnd()
        {
            Vector3[] path = CliffJumpArc.BuildWaypoints(Start, End, 5f, 4);

            Assert.AreEqual(End, path[path.Length - 1]);
        }

        [Test]
        public void BuildWaypoints_ClampsNonPositiveJumpsToOne()
        {
            Vector3[] path = CliffJumpArc.BuildWaypoints(Start, End, 3f, 0);

            Assert.AreEqual(2, path.Length);
            Assert.AreEqual(new Vector3(5f, 3f, 0f), path[0]);
            Assert.AreEqual(End, path[1]);
        }
    }
}
