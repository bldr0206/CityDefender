using System.Collections.Generic;
using NUnit.Framework;

namespace CityDef.Gameplay.Logic.Tests
{
    public sealed class QuestQueueTests
    {
        [Test]
        public void NextIncomplete_NothingCompleted_ReturnsFirst()
        {
            var ids = new List<string> { "a", "b", "c" };

            Assert.AreEqual("a", QuestQueue.NextIncomplete(ids, new HashSet<string>()));
        }

        [Test]
        public void NextIncomplete_SkipsCompletedInOrder()
        {
            var ids = new List<string> { "a", "b", "c" };
            var done = new HashSet<string> { "a", "b" };

            Assert.AreEqual("c", QuestQueue.NextIncomplete(ids, done));
        }

        [Test]
        public void NextIncomplete_CompletedOutOfOrder_ReturnsFirstUnfinished()
        {
            var ids = new List<string> { "a", "b", "c" };
            var done = new HashSet<string> { "b" };

            Assert.AreEqual("a", QuestQueue.NextIncomplete(ids, done));
        }

        [Test]
        public void NextIncomplete_AllCompleted_ReturnsNull()
        {
            var ids = new List<string> { "a", "b" };
            var done = new HashSet<string> { "a", "b" };

            Assert.IsNull(QuestQueue.NextIncomplete(ids, done));
        }

        [Test]
        public void NextIncomplete_EmptyList_ReturnsNull()
        {
            Assert.IsNull(QuestQueue.NextIncomplete(new List<string>(), new HashSet<string>()));
        }

        [Test]
        public void NextIncomplete_NullCompleted_ReturnsFirst()
        {
            var ids = new List<string> { "a", "b" };

            Assert.AreEqual("a", QuestQueue.NextIncomplete(ids, null));
        }

        [Test]
        public void NextIncomplete_NullList_ReturnsNull()
        {
            Assert.IsNull(QuestQueue.NextIncomplete(null, new HashSet<string>()));
        }
    }
}
