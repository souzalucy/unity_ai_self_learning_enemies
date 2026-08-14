using Unity.MLAgents.Sensors;
using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Observes a target transform relative to the enemy.
    /// 8 floats: relativePos(3), distance(1), angleDot(1), targetVel(3)
    /// </summary>
    public class ObsTargetTransform : ObservationSource
    {
        [Tooltip("Direct target transform. If null, checks for ITargetProvider on this GameObject.")]
        public Transform target;

        [Tooltip("Max distance for normalizing.")]
        public float maxDistance = 50f;

        public override int ObservationSize => 8;

        private ITargetProvider _targetProvider;

        private void Awake()
        {
            _targetProvider = GetComponent<ITargetProvider>();
        }

        private Transform GetTarget()
        {
            if (target != null) return target;
            if (_targetProvider != null && _targetProvider.HasValidTarget)
                return _targetProvider.Target;
            return null;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            Transform t = GetTarget();
            if (t == null)
            {
                WriteNoTarget(sensor);
                return;
            }
            WriteTarget(sensor, t);
        }

        private static void WriteNoTarget(VectorSensor sensor)
        {
            sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f);
            sensor.AddObservation(1f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f);
        }

        private void WriteTarget(VectorSensor sensor, Transform t)
        {
            Vector3 delta = t.position - transform.position;
            float dist = delta.magnitude;
            Vector3 dir = dist > 0.001f ? delta / dist : Vector3.zero;

            sensor.AddObservation(dir.x);
            sensor.AddObservation(dir.y);
            sensor.AddObservation(dir.z);
            sensor.AddObservation(Mathf.Clamp(dist / maxDistance, 0f, 1f));

            float dot = Vector3.Dot(transform.forward, dir);
            sensor.AddObservation(dot);

            Vector3 tVel = t.TryGetComponent<Rigidbody>(out var rb) ? rb.linearVelocity : Vector3.zero;
            WriteNormalizedVelocity(sensor, tVel);
        }

        private static void WriteNormalizedVelocity(VectorSensor sensor, Vector3 velocity)
        {
            sensor.AddObservation(Mathf.Clamp(velocity.x / 20f, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(velocity.y / 20f, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(velocity.z / 20f, -1f, 1f));
        }
    }
}
