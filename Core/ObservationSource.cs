using Unity.MLAgents.Sensors;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Abstract base for modular observation sources.
    /// Attach one or more subclasses to an EnemyBrain GameObject to define what the enemy perceives.
    /// </summary>
    public abstract class ObservationSource : UnityEngine.MonoBehaviour
    {
        /// <summary>
        /// Version counter — bump when observation layout changes so trainers know to re-train.
        /// </summary>
        [UnityEngine.HideInInspector]
        public int layoutVersion = 1;

        /// <summary>
        /// Number of float values this source contributes to the observation vector.
        /// Must be deterministic and never change at runtime after initialization.
        /// </summary>
        public abstract int ObservationSize { get; }

        /// <summary>
        /// Writes this source's observations into the provided VectorSensor.
        /// Called every agent decision step by EnemyBrain.
        /// </summary>
        public abstract void CollectObservations(VectorSensor sensor);

        /// <summary>
        /// Called when a new episode begins. Override to reset any internal state.
        /// </summary>
        public virtual void OnEpisodeBegin() { }

        /// <summary>
        /// Optional debug name shown in the EnemyBrain inspector.
        /// </summary>
        public virtual string SourceName => GetType().Name;
    }
}
