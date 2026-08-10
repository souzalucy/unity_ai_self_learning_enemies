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
        [Tooltip("The target transform to observe (e.g. player).")]
        public Transform target;

        [Tooltip("Max distance for normalizing. Distances beyond this clamp to 1.")]
        public float maxDistance = 50f;

        public override int ObservationSize => 8;

        public override void CollectObservations(VectorSensor sensor)
        {
            if (target == null)
            {
                sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f);
                sensor.AddObservation(1f); // max distance
                sensor.AddObservation(0f); // facing away
                sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f);
                return;
            }

            Vector3 delta = target.position - transform.position;
            float dist = delta.magnitude;

            // Relative direction (normalized)
            Vector3 dir = dist > 0.001f ? delta / dist : Vector3.zero;
            sensor.AddObservation(dir.x);
            sensor.AddObservation(dir.y);
            sensor.AddObservation(dir.z);

            // Normalized distance
            sensor.AddObservation(Mathf.Clamp(dist / maxDistance, 0f, 1f));

            // Are we facing the target? (dot product of forward vs direction to target)
            float dot = Vector3.Dot(transform.forward, dir);
            sensor.AddObservation(dot);

            // Target velocity
            Vector3 tVel = target.TryGetComponent<Rigidbody>(out var rb) ? rb.linearVelocity : Vector3.zero;
            sensor.AddObservation(Mathf.Clamp(tVel.x / 20f, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(tVel.y / 20f, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(tVel.z / 20f, -1f, 1f));
        }
    }
}
