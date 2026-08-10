using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Gaussian-shaped reward for maintaining an optimal distance from the target.
    /// Peak reward at preferredDistance, falling off to zero at maxDistance.
    /// Useful for ranged enemies that want to kite, or melee enemies that want to close in.
    /// </summary>
    public class RewardDistanceManagement : RewardSource
    {
        [Header("Distance Settings")]
        [Tooltip("The ideal distance to maintain from the target.")]
        public float preferredDistance = 8f;

        [Tooltip("Distance at which reward drops to near-zero.")]
        public float maxDistance = 30f;

        [Tooltip("Sigma for the Gaussian curve. Smaller = tighter peak.")]
        public float sigma = 5f;

        [Header("Target")]
        [Tooltip("The target to measure distance to. If null, uses a tagged object.")]
        public Transform target;

        [Tooltip("Tag to search for if no target is assigned.")]
        public string targetTag = "Player";

        private Transform _cachedTarget;

        private void Start()
        {
            if (target == null && !string.IsNullOrEmpty(targetTag))
            {
                var go = GameObject.FindGameObjectWithTag(targetTag);
                if (go != null) _cachedTarget = go.transform;
            }
            else
            {
                _cachedTarget = target;
            }
        }

        public override float CalculateReward()
        {
            Transform t = target ?? _cachedTarget;
            if (t == null) return 0f;

            float dist = Vector3.Distance(transform.position, t.position);
            float normalized = Mathf.Clamp01(dist / maxDistance);

            // Gaussian: exp(-(x-mu)^2 / (2*sigma^2))
            float mu = preferredDistance / maxDistance;
            float sig = sigma / maxDistance;
            float gaussian = Mathf.Exp(-Mathf.Pow(normalized - mu, 2) / (2f * sig * sig));

            // Scale to [-1, 1]: map gaussian from [~0, 1] to roughly [-0.2, 0.5] per step
            // so that being at perfect range gives strong positive, being far gives near zero or slight negative
            float reward = (gaussian - 0.3f) * 0.5f;

            return Mathf.Clamp(reward, -0.1f, 0.5f);
        }
    }
}
