using Unity.MLAgents.Sensors;
using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Observes the enemy's own status values (health, mana, stamina, shield, alive).
    /// 5 floats, all normalized [0,1].
    /// </summary>
    public class ObsSelfStatus : ObservationSource
    {
        [Header("Interface (preferred)")]
        [Tooltip("If assigned or found on this GameObject, reads status from this provider instead of direct fields.")]
        public IStatusProvider statusProvider;

        [Header("Direct Fields (fallback)")]
        [Tooltip("Used when no IStatusProvider is available.")]
        public float health = 100f;
        public float maxHealth = 100f;
        public float secondaryResource = 100f;
        public float maxSecondaryResource = 100f;
        public float shield = 0f;
        public float maxShield = 100f;
        public bool isAlive = true;

        public override int ObservationSize => 5;

        private void Awake()
        {
            if (statusProvider == null)
                statusProvider = GetComponent<IStatusProvider>();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            if (statusProvider != null)
            {
                WriteNormalized(sensor, statusProvider.Health, statusProvider.MaxHealth);
                WriteNormalized(sensor, statusProvider.SecondaryResource, statusProvider.MaxSecondaryResource);
                WriteNormalized(sensor, statusProvider.Shield, statusProvider.MaxShield);
                sensor.AddObservation(statusProvider.IsAlive ? 1f : 0f);
            }
            else
            {
                WriteNormalized(sensor, health, maxHealth);
                WriteNormalized(sensor, secondaryResource, maxSecondaryResource);
                WriteNormalized(sensor, shield, maxShield);
                sensor.AddObservation(isAlive ? 1f : 0f);
            }
            sensor.AddObservation(0f); // reserved
        }

        private static void WriteNormalized(VectorSensor sensor, float value, float max) =>
            sensor.AddObservation(max > 0.001f ? Mathf.Clamp01(value / max) : 0f);
    }
}
