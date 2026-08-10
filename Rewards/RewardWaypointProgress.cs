using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Racing-specific reward: rewards progress along waypoints + speed in the direction of the next waypoint.
    /// Call RegisterWaypointReached() from your track checkpoint system.
    /// </summary>
    public class RewardWaypointProgress : RewardSource
    {
        [Header("Waypoints")]
        [Tooltip("Ordered waypoint transforms along the track.")]
        public Transform[] waypoints;

        [Tooltip("Reward for reaching each waypoint.")]
        public float waypointReachedReward = 0.1f;

        [Tooltip("Bonus for completing a full lap.")]
        public float lapCompleteReward = 1.0f;

        [Header("Speed Reward")]
        [Tooltip("Reward multiplier for dot product of velocity vs waypoint direction.")]
        public float speedAlignmentMultiplier = 0.005f;

        private int _currentWaypointIndex;
        private int _lapCount;
        private int _waypointsThisStep;

        public override float CalculateReward()
        {
            float reward = 0f;

            // Waypoint-passed rewards
            reward += _waypointsThisStep * waypointReachedReward;
            _waypointsThisStep = 0;

            // Speed alignment reward
            if (waypoints != null && waypoints.Length > 0 && _currentWaypointIndex < waypoints.Length)
            {
                Transform nextWp = waypoints[_currentWaypointIndex];
                if (nextWp != null)
                {
                    Vector3 toWp = (nextWp.position - transform.position).normalized;
                    Vector3 vel = TryGetComponent<Rigidbody>(out var rb) ? rb.linearVelocity : Vector3.zero;
                    float dot = Vector3.Dot(vel.normalized, toWp);
                    float speed = vel.magnitude;
                    reward += dot * speed * speedAlignmentMultiplier;
                }
            }

            return reward;
        }

        public void RegisterWaypointReached(int waypointIndex)
        {
            _waypointsThisStep++;

            if (waypoints != null && waypoints.Length > 0)
            {
                // Check for lap completion
                if (waypointIndex == 0 && _currentWaypointIndex >= waypoints.Length - 1)
                {
                    _lapCount++;
                    // lap reward is applied separately via CalculateReward or directly
                }
                _currentWaypointIndex = waypointIndex;
            }
        }

        public void RegisterLapComplete()
        {
            // Direct reward injection
        }

        public override void OnEpisodeBegin()
        {
            _currentWaypointIndex = 0;
            _lapCount = 0;
            _waypointsThisStep = 0;
        }
    }
}
