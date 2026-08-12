using NUnit.Framework;
using UnityEngine;

namespace SelfLearningEnemies.Tests
{
    /// <summary>
    /// Tests for GenreProfile factory methods and RewardSource subclasses.
    /// </summary>
    public class GenreProfileTests
    {
        [Test]
        public void CreateRPGDefaults_SetsCorrectGenre()
        {
            var p = GenreProfile.CreateRPGDefaults();
            Assert.AreEqual(EnemyGenre.RPG, p.genre);
            Assert.AreEqual(6, p.discreteBranchSizes[0]);
            Assert.AreEqual(2, p.continuousActionCount);
            Assert.AreEqual(28, p.totalObservationSize);
            Assert.AreEqual(3, p.observationStackCount);
            Assert.AreEqual(0.005f, p.survivalRewardPerSecond, 0.0001f);
            Assert.AreEqual(-1.0f, p.deathPenalty, 0.0001f);
            Assert.AreEqual(2.0f, p.objectiveCompleteReward, 0.0001f);
            Object.DestroyImmediate(p);
        }

        [Test]
        public void CreateShooterDefaults_SetsCorrectGenre()
        {
            var p = GenreProfile.CreateShooterDefaults();
            Assert.AreEqual(EnemyGenre.Shooter, p.genre);
            Assert.IsTrue(p.useCuriosity);
            Assert.AreEqual(2, p.discreteBranchCount);
            Assert.AreEqual(4, p.continuousActionCount);
            Assert.AreEqual(48, p.totalObservationSize);
            Object.DestroyImmediate(p);
        }

        [Test]
        public void CreateRacingDefaults_SetsCorrectGenre()
        {
            var p = GenreProfile.CreateRacingDefaults();
            Assert.AreEqual(EnemyGenre.Racing, p.genre);
            Assert.AreEqual(2, p.discreteBranchCount);
            Assert.AreEqual(3, p.continuousActionCount);
            Assert.AreEqual(36, p.totalObservationSize);
            Assert.AreEqual(0.0f, p.survivalRewardPerSecond, 0.0001f);
            Object.DestroyImmediate(p);
        }

        [Test]
        public void DefaultValues_AreReasonable()
        {
            var p = ScriptableObject.CreateInstance<GenreProfile>();
            Assert.AreEqual(EnemyGenre.Custom, p.genre);
            Assert.AreEqual(5000, p.maxStep);
            Assert.AreEqual(1, p.discreteBranchCount);
            Assert.AreEqual(2, p.continuousActionCount);
            Assert.AreEqual(20, p.totalObservationSize);
            Object.DestroyImmediate(p);
        }
    }

    public class RewardSourceTests
    {
        [Test]
        public void CombatPerformance_RegisterHit_Accumulates()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardCombatPerformance>();
            r.hitReward = 0.2f;
            r.damageDealtMultiplier = 0.01f;
            r.RegisterHit(10f);
            float reward = r.CalculateReward();
            Assert.AreEqual(0.3f, reward, 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CombatPerformance_RegisterMiss_Penalizes()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardCombatPerformance>();
            r.missPenalty = -0.05f;
            r.RegisterMiss();
            float reward = r.CalculateReward();
            Assert.AreEqual(-0.05f, reward, 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CombatPerformance_RegisterKill_AddsKillReward()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardCombatPerformance>();
            r.killReward = 0.5f;
            r.RegisterKill();
            float reward = r.CalculateReward();
            Assert.AreEqual(0.5f, reward, 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CombatPerformance_FriendlyFire_Penalizes()
        {
            var go = new GameObject("T");
            var r = go.AddComponent<RewardCombatPerformance>();
            r.friendlyFirePenalty = -0.3f;
            r.RegisterFriendlyFire(50f);
            float reward = r.CalculateReward();
            Assert.AreEqual(-0.3f, reward, 0.001f);
            Object.DestroyImmediate(go);
        }
    }
}
