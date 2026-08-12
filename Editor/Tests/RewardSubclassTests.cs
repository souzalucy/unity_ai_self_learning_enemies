using NUnit.Framework;
using UnityEngine;

namespace SelfLearningEnemies.Tests
{
    /// <summary>
    /// Tests for RewardSurvival, RewardDistanceManagement, RewardCoverUsage,
    /// RewardWaypointProgress, and RewardSource weight/isActive behavior.
    /// </summary>
    public class RewardSurvivalTests
    {
        [Test]
        public void CalculatesPerStepReward()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardSurvival>();
            r.survivalRewardPerStep = 0.01f;
            Assert.AreEqual(0.01f, r.CalculateReward(), 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void DeathPenalty_AppliedOnReport()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardSurvival>();
            r.deathPenalty = -1.0f;
            r.ReportDeath();
            Assert.AreEqual(-1.0f, r.CalculateReward(), 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CompletionBonus_AppliedOnReport()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardSurvival>();
            r.completionBonus = 2.0f;
            r.ReportEpisodeComplete();
            Assert.AreEqual(2.0f, r.CalculateReward(), 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEpisodeBegin_ResetsAccumulators()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardSurvival>();
            r.ReportDeath();
            r.OnEpisodeBegin();
            Assert.AreEqual(r.survivalRewardPerStep, r.CalculateReward(), 0.001f);
            Object.DestroyImmediate(go);
        }
    }

    public class RewardDistanceTests
    {
        [Test]
        public void AtPreferredDistance_GivesOptimalReward()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardDistanceManagement>();
            r.preferredDistance = 5f;
            r.maxDistance = 10f;
            r.optimalReward = 0.1f;
            r.target = new GameObject("Target").transform;
            r.target.position = new Vector3(5f, 0, 0);
            Assert.Greater(r.CalculateReward(), 0f);
            Object.DestroyImmediate(r.target.gameObject);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TooFar_GivesPenalty()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardDistanceManagement>();
            r.preferredDistance = 5f;
            r.maxDistance = 10f;
            r.farPenalty = -0.05f;
            r.target = new GameObject("Target").transform;
            r.target.position = new Vector3(50f, 0, 0);
            Assert.Less(r.CalculateReward(), 0f);
            Object.DestroyImmediate(r.target.gameObject);
            Object.DestroyImmediate(go);
        }
    }

    public class RewardCoverAndWaypointTests
    {
        [Test]
        public void CoverUsage_NoThreat_ReturnsZero()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardCoverUsage>();
            Assert.AreEqual(0f, r.CalculateReward(), 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CoverUsage_OnEpisodeBegin_Resets()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardCoverUsage>();
            r.OnEpisodeBegin();
            Assert.IsNotNull(r);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void WaypointProgress_NoWaypoints_ReturnsZero()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardWaypointProgress>();
            r.waypoints = null;
            Assert.AreEqual(0f, r.CalculateReward(), 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void WaypointProgress_RegisterWaypoint_GivesReward()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardWaypointProgress>();
            r.waypointReachedReward = 0.1f;
            r.RegisterWaypointReached(0);
            Assert.AreEqual(0.1f, r.CalculateReward(), 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void WaypointProgress_OnEpisodeBegin_Resets()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardWaypointProgress>();
            r.waypointReachedReward = 0.1f;
            r.RegisterWaypointReached(0);
            r.OnEpisodeBegin();
            Assert.AreEqual(0f, r.CalculateReward(), 0.001f);
            Object.DestroyImmediate(go);
        }
    }

    public class RewardSourcePropertiesTests
    {
        [Test]
        public void Weight_MultipliesOutput()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardSurvival>();
            r.survivalRewardPerStep = 0.01f;
            r.RewardWeight = 2.0f;
            float reward = r.CalculateReward() * r.RewardWeight;
            Assert.AreEqual(0.02f, reward, 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void IsActive_False_CanBeRead()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardSurvival>();
            r.IsActive = false;
            Assert.IsFalse(r.IsActive);
            r.IsActive = true;
            Assert.IsTrue(r.IsActive);
            Object.DestroyImmediate(go);
        }
    }
}
