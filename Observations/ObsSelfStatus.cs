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
        [Header("Status References")]
        [Tooltip("Reference to the health component or custom script. Set via external code.")]
        public float health = 100f;
        public float maxHealth = 100f;

        [Tooltip("Optional secondary resource (mana / energy / stamina).")]
        public float secondaryResource = 100f;
        public float maxSecondaryResource = 100f;

        [Tooltip("Optional shield / armor value.")]
        public float shield = 0f;
        public float maxShield = 100f;

        [Tooltip("Is the enemy currently alive?")]
        public bool isAlive = true;

        public override int ObservationSize => 5;

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(maxHealth > 0.001f ? Mathf.Clamp01(health / maxHealth) : 0f);
            sensor.AddObservation(maxSecondaryResource > 0.001f ? Mathf.Clamp01(secondaryResource / maxSecondaryResource) : 0f);
            sensor.AddObservation(maxShield > 0.001f ? Mathf.Clamp01(shield / maxShield) : 0f);
            sensor.AddObservation(isAlive ? 1f : 0f);
            sensor.AddObservation(0f); // reserved for future use (e.g. cooldown fraction)
        }
    }
}
