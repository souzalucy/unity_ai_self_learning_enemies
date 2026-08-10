using NUnit.Framework;
using UnityEngine;

namespace SelfLearningEnemies.Tests
{
    public class BehaviorTreeTests
    {
        [Test]
        public void BTCondition_True_ReturnsSuccess()
        {
            Assert.AreEqual(BT.BTStatus.Success, new BT.BTCondition(() => true).Tick());
        }

        [Test]
        public void BTCondition_False_ReturnsFailure()
        {
            Assert.AreEqual(BT.BTStatus.Failure, new BT.BTCondition(() => false).Tick());
        }

        [Test]
        public void BTSequence_AllSuccess()
        {
            var seq = new BT.BTSequence("T",
                new BT.BTCondition(() => true), new BT.BTCondition(() => true));
            Assert.AreEqual(BT.BTStatus.Success, seq.Tick());
        }

        [Test]
        public void BTSequence_OneFailure_ReturnsFailure()
        {
            var seq = new BT.BTSequence("T",
                new BT.BTCondition(() => true), new BT.BTCondition(() => false));
            Assert.AreEqual(BT.BTStatus.Failure, seq.Tick());
        }

        [Test]
        public void BTSelector_FirstSuccess()
        {
            var sel = new BT.BTSelector("T",
                new BT.BTCondition(() => true), new BT.BTCondition(() => false));
            Assert.AreEqual(BT.BTStatus.Success, sel.Tick());
        }

        [Test]
        public void BTSelector_AllFail()
        {
            var sel = new BT.BTSelector("T",
                new BT.BTCondition(() => false), new BT.BTCondition(() => false));
            Assert.AreEqual(BT.BTStatus.Failure, sel.Tick());
        }

        [Test]
        public void BTInverter_Flips()
        {
            Assert.AreEqual(BT.BTStatus.Failure,
                new BT.BTInverter(new BT.BTCondition(() => true)).Tick());
            Assert.AreEqual(BT.BTStatus.Success,
                new BT.BTInverter(new BT.BTCondition(() => false)).Tick());
        }

        [Test]
        public void BTActionNode_Executes()
        {
            bool ok = false;
            new BT.BTActionNode(() => { ok = true; }).Tick();
            Assert.IsTrue(ok);
        }

        [Test]
        public void BTRepeater_Repeats()
        {
            int c = 0;
            var r = new BT.BTRepeater(new BT.BTActionNode(() => { c++; }), 3);
            for (int i = 0; i < 5; i++) r.Tick();
            Assert.AreEqual(5, c);
        }
    }

    public class CurriculumManagerTests
    {
        [Test]
        public void NoLessons_DoesNotThrow()
        {
            var go = new GameObject("T");
            var cm = go.AddComponent<CurriculumManager>();
            cm.lessons = null;
            Assert.DoesNotThrow(() => cm.ReportEpisodeComplete(1f));
            Assert.AreEqual(0, cm.CurrentLessonIndex);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void LessonAdvancement()
        {
            var go = new GameObject("T");
            var cm = go.AddComponent<CurriculumManager>();
            cm.autoAdvance = true;
            cm.lessons = new CurriculumLesson[] {
                new CurriculumLesson { lessonName = "Easy", completionThreshold = 0.3f, minEpisodes = 1, windowSize = 5 },
                new CurriculumLesson { lessonName = "Hard", completionThreshold = 1.0f, minEpisodes = 1, windowSize = 5 }
            };
            for (int i = 0; i < 5; i++) cm.ReportEpisodeComplete(1f);
            Assert.AreEqual(1, cm.CurrentLessonIndex);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void RollingAverage()
        {
            var go = new GameObject("T");
            var cm = go.AddComponent<CurriculumManager>();
            cm.ReportEpisodeComplete(0.5f);
            cm.ReportEpisodeComplete(1.0f);
            Assert.AreEqual(0.75f, cm.GetRollingAverageReward(), 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ResetClearsState()
        {
            var go = new GameObject("T");
            var cm = go.AddComponent<CurriculumManager>();
            cm.lessons = new CurriculumLesson[] {
                new CurriculumLesson { lessonName = "L1", completionThreshold = 0.5f, minEpisodes = 1 }
            };
            cm.ReportEpisodeComplete(1f); cm.AdvanceLesson(); cm.ResetCurriculum();
            Assert.AreEqual(0, cm.CurrentLessonIndex);
            Object.DestroyImmediate(go);
        }
    }

    public class InterfaceTests
    {
        [Test]
        public void StatusProvider_TakeDamage()
        {
            var go = new GameObject("T");
            var sp = go.AddComponent<SimpleStatusProvider>();
            sp.TakeDamage(30f);
            Assert.AreEqual(70f, sp.Health, 0.01f);
            Assert.IsTrue(sp.IsAlive);
            sp.TakeDamage(80f);
            Assert.AreEqual(0f, sp.Health, 0.01f);
            Assert.IsFalse(sp.IsAlive);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void StatusProvider_Heal()
        {
            var go = new GameObject("T");
            var sp = go.AddComponent<SimpleStatusProvider>();
            sp.TakeDamage(50f);
            sp.Heal(30f);
            Assert.AreEqual(80f, sp.Health, 0.01f);
            Object.DestroyImmediate(go);
        }
    }
}
