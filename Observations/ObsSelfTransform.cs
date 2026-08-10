using Unity.MLAgents.Sensors;
using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Observes the enemy's own transform: position, rotation, and velocity.
    /// 7 floats: localPos(3), forward(3), speed(1)
    /// </summary>
    public class ObsSelfTransform : ObservationSource
    {
        [Tooltip("Optional arena bounds for normalizing position to [-1,1].")]
        public Vector3 arenaSize = new Vector3(50f, 0f, 50f);

        [Tooltip("Reference transform. Defaults to this GameObject.")]
        public Transform selfTransform;

        public override int ObservationSize => 7;

        private void Awake()
        {
            if (selfTransform == null)
                selfTransform = transform;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            Vector3 pos = selfTransform.position;
            Vector3 fwd = selfTransform.forward;
            Vector3 vel = selfTransform.TryGetComponent<Rigidbody>(out var rb) ? rb.linearVelocity : Vector3.zero;

            // Normalized position
            sensor.AddObservation(arenaSize.x > 0.001f ? pos.x / arenaSize.x : 0f);
            sensor.AddObservation(arenaSize.y > 0.001f ? pos.y / arenaSize.y : 0f);
            sensor.AddObservation(arenaSize.z > 0.001f ? pos.z / arenaSize.z : 0f);

            // Forward direction
            sensor.AddObservation(fwd.x);
            sensor.AddObservation(fwd.y);
            sensor.AddObservation(fwd.z);

            // Speed (normalized by a reasonable max speed)
            float speed = vel.magnitude;
            sensor.AddObservation(Mathf.Clamp(speed / 20f, -1f, 1f));
        }
    }
}
