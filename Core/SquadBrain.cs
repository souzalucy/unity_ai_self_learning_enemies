using Unity.MLAgents;
using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Manages a squad/team of EnemyBrain agents that share a cooperative reward signal.
    /// Uses ML-Agents' SimpleMultiAgentGroup for group-based training.
    ///
    /// Usage:
    ///   1. Add SquadBrain to a GameObject in the scene
    ///   2. Assign all squad member EnemyBrain components (or call RegisterAgent)
    ///   3. Call SquadBrain.ReportGroupReward() from your game logic for team achievements
    ///   4. Call SquadBrain.EndGroupEpisode() when the squad's episode is over
    ///
    /// The group reward is distributed equally among all registered agents.
    /// Individual rewards from RewardSource components still apply per-agent.
    /// </summary>
    public class SquadBrain : MonoBehaviour
    {
        [Header("Squad Members")]
        [Tooltip("All EnemyBrain agents in this squad.")]
        public EnemyBrain[] squadMembers;

        [Header("Group Rewards")]
        [Tooltip("Reward given to each squad member when the group achieves an objective.")]
        public float groupObjectiveReward = 1f;

        [Tooltip("Penalty given to each squad member on group failure.")]
        public float groupFailurePenalty = -1f;

        [Tooltip("Small shared reward per step for being near allies (encourages grouping).")]
        public float proximityRewardPerStep = 0.001f;

        [Tooltip("Max distance for proximity reward between squad members.")]
        public float proximityRange = 10f;

        private SimpleMultiAgentGroup _agentGroup;
        private int _registeredCount;

        private void Start()
        {
            _agentGroup = new SimpleMultiAgentGroup();

            if (squadMembers != null)
            {
                foreach (var member in squadMembers)
                {
                    if (member != null)
                    {
                        _agentGroup.RegisterAgent(member);
                        _registeredCount++;
                    }
                }
            }

            if (_registeredCount == 0)
                Debug.LogWarning("[SquadBrain] No squad members registered. Use RegisterAgent() or assign squadMembers.");
        }

        /// <summary>
        /// Register an additional agent to this squad at runtime.
        /// </summary>
        public void RegisterAgent(EnemyBrain agent)
        {
            if (agent == null) return;
            _agentGroup.RegisterAgent(agent);
            _registeredCount++;
            if (squadMembers == null)
                squadMembers = new EnemyBrain[] { agent };
        }

        /// <summary>
        /// Remove an agent from the squad.
        /// </summary>
        public void UnregisterAgent(EnemyBrain agent)
        {
            if (agent == null) return;
            _agentGroup.UnregisterAgent(agent);
            _registeredCount--;
        }

        /// <summary>
        /// Give a shared reward to all squad members (e.g., team captured a point).
        /// </summary>
        public void ReportGroupReward(float reward)
        {
            _agentGroup.AddGroupReward(reward);
        }

        /// <summary>
        /// Report that the squad completed its objective.
        /// </summary>
        public void ReportGroupObjectiveComplete()
        {
            _agentGroup.AddGroupReward(groupObjectiveReward);
            _agentGroup.EndGroupEpisode();
        }

        /// <summary>
        /// Report that the squad failed.
        /// </summary>
        public void ReportGroupFailure()
        {
            _agentGroup.AddGroupReward(groupFailurePenalty);
            _agentGroup.EndGroupEpisode();
        }

        /// <summary>
        /// End the episode for all squad members.
        /// </summary>
        public void EndGroupEpisode()
        {
            _agentGroup.EndGroupEpisode();
        }

        /// <summary>
        /// Call each step to reward squad members for staying close together.
        /// </summary>
        public void ApplyProximityRewards()
        {
            if (squadMembers == null || squadMembers.Length < 2) return;

            for (int i = 0; i < squadMembers.Length; i++)
            {
                if (squadMembers[i] == null) continue;
                for (int j = i + 1; j < squadMembers.Length; j++)
                {
                    if (squadMembers[j] == null) continue;
                    float dist = Vector3.Distance(
                        squadMembers[i].transform.position,
                        squadMembers[j].transform.position);

                    if (dist < proximityRange)
                    {
                        float reward = proximityRewardPerStep * (1f - dist / proximityRange);
                        squadMembers[i].AddReward(reward);
                        squadMembers[j].AddReward(reward);
                    }
                }
            }
        }

        /// <summary>
        /// Get the number of currently registered squad members.
        /// </summary>
        public int MemberCount => _registeredCount;

        private void OnDestroy()
        {
            if (_agentGroup != null)
            {
                // Unregister all agents
                if (squadMembers != null)
                    foreach (var m in squadMembers)
                        if (m != null) _agentGroup.UnregisterAgent(m);
            }
        }
    }
}
