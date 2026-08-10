using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SelfLearningEnemies.Tests
{
    /// <summary>
    /// Unit tests for EnemyBrain component discovery, action mapping, and safety.
    /// Run from Unity Test Runner: Window → General → Test Runner → EditMode
    /// </summary>
    public class EnemyBrainTests
    {
        private GameObject _testObj;
        private EnemyBrain _brain;

        [SetUp]
        public void Setup()
        {
            _testObj = new GameObject("TestEnemy");
            _brain = _testObj.AddComponent<EnemyBrain>();
            _testObj.AddComponent<Unity.MLAgents.Policies.BehaviorParameters>();
            _testObj.AddComponent<Unity.MLAgents.DecisionRequester>();
        }

        [TearDown]
        public void Teardown() { Object.DestroyImmediate(_testObj); }

        [Test]
        public void CacheComponents_NoSources_ReturnsEmpty()
        {
            _brain.CacheComponents();
            Assert.AreEqual(0, _brain.GetComponents<ObservationSource>().Length);
            Assert.AreEqual(0, _brain.GetComponents<ActionEffect>().Length);
            Assert.AreEqual(0, _brain.GetComponents<RewardSource>().Length);
        }

        [Test]
        public void CacheComponents_FindsAllSources()
        {
            _testObj.AddComponent<ObsSelfTransform>();
            _testObj.AddComponent<ObsSelfStatus>();
            _testObj.AddComponent<ActionNavMeshMovement>();
            _testObj.AddComponent<RewardSurvival>();
            _brain.CacheComponents();
            Assert.AreEqual(2, _brain.GetComponents<ObservationSource>().Length);
            Assert.AreEqual(1, _brain.GetComponents<ActionEffect>().Length);
            Assert.AreEqual(1, _brain.GetComponents<RewardSource>().Length);
        }

        [Test]
        public void GetTotalObservationSize_SumsCorrectly()
        {
            _testObj.AddComponent<ObsSelfTransform>(); // 7
            _testObj.AddComponent<ObsSelfStatus>();    // 5
            _brain.CacheComponents();
            Assert.AreEqual(12, _brain.GetTotalObservationSize());
        }

        [Test]
        public void ActionCombat_BranchSize()
        {
            var c = _testObj.AddComponent<ActionCombat>();
            c.actionSlotCount = 3;
            Assert.AreEqual(1, c.DiscreteBranchCount);
            Assert.AreEqual(new int[] { 4 }, c.DiscreteBranchSizes);
        }

        [Test]
        public void ActionNavMeshMovement_ContinuousCount()
        {
            var m = _testObj.AddComponent<ActionNavMeshMovement>();
            Assert.AreEqual(0, m.DiscreteBranchCount);
            Assert.AreEqual(2, m.ContinuousActionCount);
        }

        [Test]
        public void SafeRefresh_NoChange_ReturnsFalse()
        {
            _brain.CacheComponents();
            Assert.IsFalse(_brain.SafeRefreshComponents());
        }

        [Test]
        public void SafeRefresh_ComponentAdded_ReturnsTrue()
        {
            _brain.CacheComponents();
            _testObj.AddComponent<ObsSelfTransform>();
            Assert.IsTrue(_brain.SafeRefreshComponents());
        }

        [Test]
        public void SafeRefresh_ReentrantGuard()
        {
            _brain.CacheComponents();
            _testObj.AddComponent<ObsSelfTransform>();
            Assert.IsTrue(_brain.SafeRefreshComponents());
            Assert.IsFalse(_brain.SafeRefreshComponents());
        }

        [Test]
        public void ObsSizes_AreCorrect()
        {
            Assert.AreEqual(7, _testObj.AddComponent<ObsSelfTransform>().ObservationSize);
            Assert.AreEqual(5, _testObj.AddComponent<ObsSelfStatus>().ObservationSize);
            var rays = _testObj.AddComponent<ObsRaycastPerception>();
            rays.numRays = 8;
            Assert.AreEqual(40, rays.ObservationSize);
        }

        [Test]
        public void ValidateSetup_Empty_ReturnsFalse()
        {
            _brain.CacheComponents();
            Assert.IsFalse(_brain.ValidateSetup());
        }

        [Test]
        public void ValidateSetup_WithComponents_ReturnsTrue()
        {
            _testObj.AddComponent<ObsSelfTransform>();
            _testObj.AddComponent<ActionNavMeshMovement>();
            _brain.CacheComponents();
            Assert.IsTrue(_brain.ValidateSetup());
        }

        [Test]
        public void RewardSource_CanDisable()
        {
            var r = _testObj.AddComponent<RewardSurvival>();
            r.IsActive = false;
            Assert.IsFalse(r.IsActive);
        }
    }
}
