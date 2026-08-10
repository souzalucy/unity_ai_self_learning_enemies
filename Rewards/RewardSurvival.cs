using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Rewards the enemy for staying alive and penalizes death.
    /// Small positive reward each step + large negative on episode end.
    /// </summary>
    public class RewardSurvival : RewardSource
    {
        [Header("Reward Values")]
        [Tooltip("Reward per second of survival (added each step).")]
        public float survivalRate = 0.005f;

        [Tooltip("Large penalty applied on death (call ReportDeath()).")]
        public float deathPenalty = -1.0f;

        [Tooltip("Bonus for reaching the end of an episode alive.")]
        public float episodeCompleteBonus = 0.5f;

        private float _accumulated;
        private bool _diedThisStep;
        private bool _episodeCompletedThisStep;

        public override float CalculateReward()
        {
            float reward = survivalRate;

            if (_diedThisStep)
            {
                reward += deathPenalty;
                _diedThisStep = false;
            }

            if (_episodeCompletedThisStep)
            {
                reward += episodeCompleteBonus;
                _episodeCompletedThisStep = false;
            }

            _accumulated += reward;
            return reward;
        }

        public void ReportDeath()
        {
            _diedThisStep = true;
        }

        public void ReportEpisodeComplete()
        {
            _episodeCompletedThisStep = true;
        }

        public override void OnEpisodeBegin()
        {
            _accumulated = 0f;
            _diedThisStep = false;
            _episodeCompletedThisStep = false;
        }
    }
}
