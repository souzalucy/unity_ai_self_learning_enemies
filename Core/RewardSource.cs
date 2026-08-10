namespace SelfLearningEnemies
{
    /// <summary>
    /// Abstract base for modular reward sources.
    /// Attach one or more subclasses to an EnemyBrain GameObject to define how the enemy is scored.
    /// </summary>
    public abstract class RewardSource : UnityEngine.MonoBehaviour
    {
        /// <summary>
        /// Weight multiplier applied to this source's reward (default 1.0).
        /// Use this to tune the importance of different reward signals without changing code.
        /// </summary>
        [UnityEngine.SerializeField]
        [UnityEngine.Range(0f, 10f)]
        protected float rewardWeight = 1.0f;

        /// <summary>
        /// Whether this source is currently active.
        /// </summary>
        [UnityEngine.SerializeField]
        protected bool isActive = true;

        /// <summary>
        /// The raw reward weight (exposed for EnemyBrain to read).
        /// </summary>
        public float RewardWeight => rewardWeight;

        /// <summary>
        /// Whether this reward source is active.
        /// </summary>
        public bool IsActive
        {
            get => isActive;
            set => isActive = value;
        }

        /// <summary>
        /// Calculate the reward value for the current step.
        /// Called every agent step by EnemyBrain. The result is multiplied by rewardWeight automatically.
        /// </summary>
        /// <returns>Raw reward value (will be multiplied by rewardWeight).</returns>
        public abstract float CalculateReward();

        /// <summary>
        /// Called when a new episode begins. Override to reset any internal tracking state.
        /// </summary>
        public virtual void OnEpisodeBegin() { }

        /// <summary>
        /// Optional debug name shown in the EnemyBrain inspector.
        /// </summary>
        public virtual string SourceName => GetType().Name;
    }
}
