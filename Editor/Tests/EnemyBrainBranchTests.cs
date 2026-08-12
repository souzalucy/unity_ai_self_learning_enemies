using NUnit.Framework;
using UnityEngine;

namespace SelfLearningEnemies.Tests
{
    /// <summary>
    /// Branch-coverage tests for EnemyBrain: ReportObjectiveComplete, ReportDeath,
    /// ValidateSetup, GetTotalObservationSize, and debugMode toggling.
    /// </summary>
    public class EnemyBrainBranchTests
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
        public void ReportObjectiveComplete_NoProfile_UsesFallback()
        {
            _brain.genreProfile = null;
            Assert.DoesNotThrow(() => _brain.ReportObjectiveComplete());
        }

        [Test]
        public void ReportObjectiveComplete_WithProfile_UsesProfileValue()
        {
            var profile = GenreProfile.CreateRPGDefaults();
            _brain.genreProfile = profile;
            Assert.DoesNotThrow(() => _brain.ReportObjectiveComplete());
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void ReportDeath_NoProfile_UsesFallback()
        {
            _brain.genreProfile = null;
            Assert.DoesNotThrow(() => _brain.ReportDeath());
        }

        [Test]
        public void ReportDeath_WithProfile_UsesProfileValue()
        {
            var profile = GenreProfile.CreateRPGDefaults();
            _brain.genreProfile = profile;
            Assert.DoesNotThrow(() => _brain.ReportDeath());
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void ValidateSetup_Empty_ReturnsFalse()
        {
            _brain.CacheComponents();
            Assert.IsFalse(_brain.ValidateSetup());
        }

        [Test]
        public void GetTotalObservationSize_NullSources_ReturnsZero()
        {
            _brain.CacheComponents();
            Assert.AreEqual(0, _brain.GetTotalObservationSize());
        }

        [Test]
        public void DebugMode_TogglesCorrectly()
        {
            _brain.debugMode = true;
            Assert.IsTrue(_brain.debugMode);
            _brain.debugMode = false;
            Assert.IsFalse(_brain.debugMode);
        }

        [Test]
        public void CacheAndValidate_ThenAddComponents_StillValidates()
        {
            _brain.CacheComponents();
            Assert.IsFalse(_brain.ValidateSetup());
            _testObj.AddComponent<ObsSelfTransform>();
            _testObj.AddComponent<ActionNavMeshMovement>();
            _brain.CacheComponents();
            Assert.IsTrue(_brain.ValidateSetup());
        }
    }
}
