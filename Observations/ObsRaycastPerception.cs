using Unity.MLAgents.Sensors;
using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Raycast-based perception: fires N rays over a configurable FOV and reports hit data.
    /// Each ray contributes 5 floats: hitDistance(1) + hitTagOneHot(4).
    /// Total size = numRays * 5.
    /// </summary>
    public class ObsRaycastPerception : ObservationSource
    {
        [Header("Raycast Setup")]
        [Tooltip("Number of rays evenly spaced across the FOV.")]
        [Range(1, 32)]
        public int numRays = 8;

        [Tooltip("Total field of view in degrees.")]
        [Range(10f, 360f)]
        public float fieldOfView = 120f;

        [Tooltip("Maximum raycast distance.")]
        public float maxDistance = 30f;

        [Tooltip("Layers the rays can hit.")]
        public LayerMask raycastMask = -1;

        [Tooltip("Vertical offset from the enemy's position (eye height).")]
        public float eyeHeight = 1.5f;

        [Header("Tag One-Hot Encoding")]
        [Tooltip("Tags to encode. Order defines the one-hot index.")]
        public string[] encodedTags = new string[] { "Player", "Enemy", "Wall", "Pickup" };

        public override int ObservationSize => numRays * 5;

        public override void CollectObservations(VectorSensor sensor)
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            float halfFov = fieldOfView * 0.5f;

            for (int i = 0; i < numRays; i++)
                WriteRay(sensor, origin, RayDirection(i, halfFov));
        }

        private Vector3 RayDirection(int i, float halfFov)
        {
            float angle = numRays > 1
                ? -halfFov + (fieldOfView / (numRays - 1)) * i
                : 0f;
            return Quaternion.Euler(0f, angle, 0f) * transform.forward;
        }

        private void WriteRay(VectorSensor sensor, Vector3 origin, Vector3 dir)
        {
            float hitDist = 1f; // 1 = max distance (no hit)
            float[] oneHot = new float[4];

            if (Physics.Raycast(origin, dir, out RaycastHit hit, maxDistance, raycastMask))
            {
                hitDist = Mathf.Clamp01(hit.distance / maxDistance);

                // Encode tag as one-hot
                for (int t = 0; t < encodedTags.Length && t < 4; t++)
                {
                    if (hit.collider.CompareTag(encodedTags[t]))
                    {
                        oneHot[t] = 1f;
                        break;
                    }
                }
            }

            sensor.AddObservation(hitDist);
            for (int t = 0; t < 4; t++)
                sensor.AddObservation(oneHot[t]);
        }
    }
}
