using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Interface for providing target information to observation and reward components.
    /// Implement this on your targeting/enemy-controller component to decouple the AI
    /// from hard-coded Transform references.
    ///
    /// Components like ObsTargetTransform, RewardDistanceManagement, and RewardCoverUsage
    /// will auto-detect and use an ITargetProvider if one exists on the same GameObject.
    /// </summary>
    public interface ITargetProvider
    {
        /// <summary>The current target's Transform (may be null).</summary>
        Transform Target { get; }

        /// <summary>World position of the current target.</summary>
        Vector3 TargetPosition { get; }

        /// <summary>Distance to the current target.</summary>
        float TargetDistance { get; }

        /// <summary>Whether there is currently a valid target.</summary>
        bool HasValidTarget { get; }

        /// <summary>Direction from self to target (normalized).</summary>
        Vector3 DirectionToTarget { get; }

        /// <summary>
        /// Optional: line-of-sight check to target (true if visible).
        /// </summary>
        bool HasLineOfSight { get; }

        /// <summary>Fired when a new target is acquired.</summary>
        event System.Action<Transform> OnTargetAcquired;

        /// <summary>Fired when the current target is lost.</summary>
        event System.Action OnTargetLost;
    }

    /// <summary>
    /// Simple default implementation that searches for a tagged GameObject.
    /// Replace with your own targeting logic (threat tables, proximity, etc.)
    /// </summary>
    public class SimpleTargetProvider : MonoBehaviour, ITargetProvider
    {
        [Tooltip("Tag to search for.")]
        public string targetTag = "Player";

        [Tooltip("Max detection range.")]
        public float detectionRange = 50f;

        [Tooltip("Layer mask for line-of-sight checks.")]
        public LayerMask losBlockMask = -1;

        private Transform _target;
        private bool _hasLOS;

        public Transform Target => _target;
        public Vector3 TargetPosition => _target != null ? _target.position : Vector3.zero;
        public float TargetDistance => _target != null ? Vector3.Distance(transform.position, _target.position) : float.MaxValue;
        public bool HasValidTarget => _target != null && TargetDistance <= detectionRange;
        public Vector3 DirectionToTarget => HasValidTarget ? (_target.position - transform.position).normalized : Vector3.forward;
        public bool HasLineOfSight => _hasLOS;

        public event System.Action<Transform> OnTargetAcquired;
        public event System.Action OnTargetLost;

        private void Update()
        {
            UpdateTarget();
        }

        private void UpdateTarget()
        {
            Transform newTarget = FilterByRange(FindTaggedTarget());
            _hasLOS = HasLineOfSight(newTarget);
            ApplyTargetChange(newTarget);
        }

        private Transform FindTaggedTarget()
        {
            var go = GameObject.FindGameObjectWithTag(targetTag);
            return go != null ? go.transform : null;
        }

        private Transform FilterByRange(Transform target)
        {
            if (target == null) return null;
            float dist = Vector3.Distance(transform.position, target.position);
            return dist > detectionRange ? null : target;
        }

        private bool HasLineOfSight(Transform target)
        {
            if (target == null) return false;
            Vector3 dir = (target.position - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, target.position);
            return !Physics.Raycast(transform.position, dir, dist, losBlockMask);
        }

        private void ApplyTargetChange(Transform newTarget)
        {
            if (newTarget == _target) return;

            if (_target != null) OnTargetLost?.Invoke();
            _target = newTarget;
            if (_target != null) OnTargetAcquired?.Invoke(_target);
        }
    }
}
