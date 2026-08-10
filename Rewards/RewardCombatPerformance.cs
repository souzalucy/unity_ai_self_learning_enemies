using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Reward shaping based on combat performance.
    /// +reward for hits and kills, -reward for misses and friendly fire.
    /// Connect to your combat system by calling RegisterHit/RegisterMiss/RegisterKill from your damage pipeline.
    /// </summary>
    public class RewardCombatPerformance : RewardSource
    {
        [Header("Reward Values")]
        public float hitReward = 0.2f;
        public float killReward = 0.5f;
        public float missPenalty = -0.05f;
        public float friendlyFirePenalty = -0.3f;
        public float damageDealtMultiplier = 0.01f; // per point of damage

        [Header("Step Tracking")]
        private float _stepHitReward;
        private float _stepKillReward;
        private float _stepMissPenalty;
        private float _stepFriendlyFirePenalty;
        private float _stepDamageReward;

        public void RegisterHit(float damageDealt, bool wasKill = false)
        {
            _stepHitReward += hitReward;
            _stepDamageReward += damageDealt * damageDealtMultiplier;
            if (wasKill) _stepKillReward += killReward;
        }

        public void RegisterMiss()
        {
            _stepMissPenalty += missPenalty;
        }

        public void RegisterFriendlyFire(float damage)
        {
            _stepFriendlyFirePenalty += friendlyFirePenalty;
        }

        public void RegisterKill()
        {
            _stepKillReward += killReward;
        }

        public override float CalculateReward()
        {
            float total = _stepHitReward + _stepKillReward + _stepMissPenalty +
                          _stepFriendlyFirePenalty + _stepDamageReward;

            // Reset per-step accumulators
            _stepHitReward = 0f;
            _stepKillReward = 0f;
            _stepMissPenalty = 0f;
            _stepFriendlyFirePenalty = 0f;
            _stepDamageReward = 0f;

            return total;
        }

        public override void OnEpisodeBegin()
        {
            _stepHitReward = 0f;
            _stepKillReward = 0f;
            _stepMissPenalty = 0f;
            _stepFriendlyFirePenalty = 0f;
            _stepDamageReward = 0f;
        }
    }
}
