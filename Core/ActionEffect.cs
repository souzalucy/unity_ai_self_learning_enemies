namespace SelfLearningEnemies
{
    /// <summary>
    /// Abstract base for modular action effectors.
    /// Attach one or more subclasses to an EnemyBrain GameObject to define what the enemy can do.
    /// </summary>
    public abstract class ActionEffect : UnityEngine.MonoBehaviour
    {
        /// <summary>
        /// Number of discrete action branches this effect consumes.
        /// Each branch represents a mutually exclusive choice (e.g. which attack to use).
        /// </summary>
        public abstract int DiscreteBranchCount { get; }

        /// <summary>
        /// Size of each discrete branch.  Must have length == DiscreteBranchCount.
        /// Example: [3] = one branch with 3 choices; [5, 4] = two branches with 5 and 4 choices.
        /// </summary>
        public abstract int[] DiscreteBranchSizes { get; }

        /// <summary>
        /// Number of continuous action floats this effect consumes.
        /// Values are in [-1, 1] by default (ML-Agents clamping).
        /// </summary>
        public abstract int ContinuousActionCount { get; }

        /// <summary>
        /// Apply the actions for this effect. Called every agent decision step by EnemyBrain.
        /// </summary>
        /// <param name="discreteActions">Slice of the full discrete action array belonging to this effect.</param>
        /// <param name="continuousActions">Slice of the full continuous action array belonging to this effect.</param>
        public abstract void ApplyActions(float[] discreteActions, float[] continuousActions);

        /// <summary>
        /// Called when a new episode begins. Override to reset any internal state.
        /// </summary>
        public virtual void OnEpisodeBegin() { }

        /// <summary>
        /// Optional debug name shown in the EnemyBrain inspector.
        /// </summary>
        public virtual string EffectName => GetType().Name;
    }
}
