using Unity.MLAgents.Sensors;
using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Observes recent sound events within hearing range.
    /// Reports the N loudest/most-recent sounds as: relativePos(3), intensity(1), typeOneHot(8) = 12 floats each.
    /// Total size = maxSounds * 12.
    /// Requires SoundEventManager in the scene (auto-created if missing).
    /// </summary>
    public class ObsSoundPerception : ObservationSource
    {
        [Header("Hearing Settings")]
        [Tooltip("Maximum hearing range in world units.")]
        public float hearingRange = 30f;

        [Tooltip("Maximum number of sound events to report per observation.")]
        [Range(1, 8)]
        public int maxSounds = 4;

        [Tooltip("Minimum intensity for a sound to be perceived.")]
        [Range(0f, 1f)]
        public float intensityThreshold = 0.1f;

        [Tooltip("Attenuation: sound intensity drops by this factor per unit distance.")]
        public float attenuation = 0.05f;

        public override int ObservationSize => maxSounds * 12; // 3(pos) + 1(intensity) + 8(type one-hot)

        public override void CollectObservations(VectorSensor sensor)
        {
            var manager = SoundEventManager.Instance;
            var sounds = manager.GetRecentSounds(transform.position, hearingRange, maxSounds);

            int reported = 0;
            foreach (var s in sounds)
            {
                // Apply distance attenuation
                float dist = Vector3.Distance(transform.position, s.position);
                float attenuatedIntensity = s.intensity * Mathf.Max(0f, 1f - dist * attenuation);

                if (attenuatedIntensity < intensityThreshold)
                    continue;

                // Relative position (normalized by hearing range)
                Vector3 rel = (s.position - transform.position) / hearingRange;
                sensor.AddObservation(Mathf.Clamp(rel.x, -1f, 1f));
                sensor.AddObservation(Mathf.Clamp(rel.y, -1f, 1f));
                sensor.AddObservation(Mathf.Clamp(rel.z, -1f, 1f));

                // Attenuated intensity
                sensor.AddObservation(attenuatedIntensity);

                // Sound type one-hot (8 categories)
                int typeIdx = (int)s.type;
                for (int t = 0; t < 8; t++)
                    sensor.AddObservation(t == typeIdx ? 1f : 0f);

                reported++;
                if (reported >= maxSounds) break;
            }

            // Pad remaining slots with zeros
            for (int i = reported; i < maxSounds; i++)
            {
                sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f); // pos
                sensor.AddObservation(0f); // intensity
                for (int t = 0; t < 8; t++) sensor.AddObservation(0f); // one-hot
            }
        }
    }
}
