using NUnit.Framework;
using UnityEngine;

namespace SelfLearningEnemies.Tests
{
    /// <summary>
    /// Tests for the minigame integration layer (Shared + Racing components).
    /// Run from Unity Test Runner: Window → General → Test Runner → EditMode.
    /// </summary>
    public class MinigameSettingsTests
    {
        [Test]
        public void ResolveProfile_ReturnsFactoryDefault_WhenNoProfileAssigned()
        {
            var s = ScriptableObject.CreateInstance<Minigames.MinigameSettings>();
            s.genre = EnemyGenre.RPG;
            var p = s.ResolveProfile();
            Assert.AreEqual(EnemyGenre.RPG, p.genre);
            Assert.AreEqual(new int[] { 6 }, p.discreteBranchSizes);
            Object.DestroyImmediate(s);
        }

        [Test]
        public void ResolveProfile_PrefersAssignedProfile()
        {
            var s = ScriptableObject.CreateInstance<Minigames.MinigameSettings>();
            s.genre = EnemyGenre.RPG;
            var assigned = GenreProfile.CreateRacingDefaults();
            s.genreProfile = assigned;
            Assert.AreSame(assigned, s.ResolveProfile());
            Object.DestroyImmediate(s);
            Object.DestroyImmediate(assigned);
        }
    }

    public class ActionAimAndShootTests
    {
        [Test]
        public void BranchAndContinuousCounts_MatchShooterProfile()
        {
            var go = new GameObject("T");
            var aim = go.AddComponent<Minigames.ActionAimAndShoot>();
            aim.combatSlotCount = 4;
            Assert.AreEqual(1, aim.DiscreteBranchCount);
            Assert.AreEqual(new int[] { 5 }, aim.DiscreteBranchSizes);
            Assert.AreEqual(2, aim.ContinuousActionCount);
            Object.DestroyImmediate(go);
        }
    }

    public class StatusDamageReceiverTests
    {
        [Test]
        public void TakeDamage_ForwardsToStatusProvider()
        {
            var go = new GameObject("T");
            var status = go.AddComponent<SimpleStatusProvider>();
            var receiver = go.AddComponent<Minigames.StatusDamageReceiver>();

            receiver.TakeDamage(30f);
            Assert.AreEqual(70f, status.Health, 0.01f);
            Assert.IsTrue(status.IsAlive);
            Object.DestroyImmediate(go);
        }
    }

    public class TrackCheckpointTests
    {
        [Test]
        public void WaypointIndex_IsAssignable()
        {
            var go = new GameObject("T");
            var col = go.AddComponent<BoxCollider>();
            var cp = go.AddComponent<Minigames.TrackCheckpoint>();
            cp.waypointIndex = 3;
            Assert.AreEqual(3, cp.waypointIndex);
            Assert.IsTrue(col.isTrigger);
            Object.DestroyImmediate(go);
        }
    }
}
