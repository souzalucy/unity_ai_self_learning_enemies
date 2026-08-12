using System;
using Unity.MLAgents.Actuators;
using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Observation collection, action dispatch, and reward calculation for each step.
    /// </summary>
    public partial class EnemyBrain
    {
        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            _episodeStep++;
            SafeRefreshComponents();

            float[] discAll = actionBuffers.DiscreteActions.Array ?? Array.Empty<float>();
            float[] contAll = actionBuffers.ContinuousActions.Array ?? Array.Empty<float>();
            DispatchActions(discAll, contAll);

            float stepReward = CalculateStepReward();
            AddReward(stepReward);
            _episodeReward += stepReward;
        }

        private float CalculateStepReward()
        {
            float stepReward = 0f;
            foreach (var rwd in _rewardSources)
            {
                if (rwd == null || !rwd.IsActive) continue;
                stepReward += rwd.CalculateReward() * rwd.RewardWeight;
            }
            if (genreProfile != null)
                stepReward += genreProfile.survivalRewardPerSecond * Time.fixedDeltaTime;
            return stepReward;
        }
    }
}