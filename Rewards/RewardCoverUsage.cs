using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Rewards the enemy for using cover effectively.
    /// Cover is detected by raycasting from the enemy toward the player; if the ray hits
    /// a "cover" object before reaching the player, the enemy is considered "in cover."
    /// </summary>
    public class RewardCoverUsage : RewardSource
    {
        [Header("Cover Detection")]
        [Tooltip("The player/threat transform.")]
        public Transform threat;

        [Tooltip("Tag that identifies cover objects.")]
        public string coverTag = "Cover";

        [Tooltip("Layers to check for cover.")]
        public LayerMask coverLayers = -1;

        [Tooltip("Max distance from enemy to check for threat.")]
        public float maxThreatDistance = 50f;

        [Header("Reward Values")]
        [Tooltip("Reward per step while in cover.")]
        public float inCoverReward = 0.02f;

        [Tooltip("Penalty per step while exposed.")]
        public float exposedPenalty = -0.01f;

        [Tooltip("Bonus for entering cover (first step after being exposed).")]
        public float enterCoverBonus = 0.1f;

        private bool _wasInCover;
        private Transform _cachedThreat;

        private void Start()
        {
            if (threat == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) _cachedThreat = go.transform;
            }
            else
            {
                _cachedThreat = threat;
            }
        }

        public override float CalculateReward()
        {
            Transform t = threat ?? _cachedThreat;
            if (t == null) return 0f;

            float dist = Vector3.Distance(transform.position, t.position);
            if (dist > maxThreatDistance) return 0f;

            Vector3 dirToThreat = (t.position - transform.position).normalized;

            bool inCover = false;
            if (Physics.Raycast(transform.position, dirToThreat, out RaycastHit hit, dist, coverLayers))
            {
                if (hit.collider.CompareTag(coverTag))
                    inCover = true;
            }

            float reward = 0f;

            if (inCover)
            {
                reward += inCoverReward;
                if (!_wasInCover)
                    reward += enterCoverBonus;
            }
            else
            {
                reward += exposedPenalty;
            }

            _wasInCover = inCover;
            return reward;
        }

        public override void OnEpisodeBegin()
        {
            _wasInCover = false;
        }
    }
}
