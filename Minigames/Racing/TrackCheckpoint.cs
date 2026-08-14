using UnityEngine;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>
    /// Trigger placed on each racing waypoint. When a car crosses it, the checkpoint
    /// updates the car's RewardWaypointProgress / ObsWaypointProgress and reports the
    /// crossing to MinigameManager for lap counting.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TrackCheckpoint : MonoBehaviour
    {
        [Tooltip("Index of this waypoint in the track order (0 = start/finish).")]
        public int waypointIndex;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            Transform root = other.transform.root;
            if (root == null) root = other.transform;

            bool isVehicle = root.CompareTag("Player") || root.CompareTag("Enemy")
                || root.GetComponentInParent<EnemyBrain>() != null;
            if (!isVehicle) return;

            var rwp = root.GetComponentInParent<RewardWaypointProgress>();
            if (rwp != null) rwp.RegisterWaypointReached(waypointIndex);

            var obs = root.GetComponentInParent<ObsWaypointProgress>();
            if (obs != null) obs.currentWaypointIndex = waypointIndex;

            if (MinigameManager.Instance != null)
                MinigameManager.Instance.OnCheckpointPassed(root.gameObject, waypointIndex);
        }
    }
}
