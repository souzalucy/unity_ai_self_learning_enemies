namespace SelfLearningEnemies
{
    /// <summary>
    /// Read-only debug/telemetry accessors for EnemyBrain.
    /// Lets game-loop managers and HUDs read the current episode reward, step count,
    /// and the most recent action buffer without touching the training internals.
    /// </summary>
    public partial class EnemyBrain
    {
        /// <summary>Cumulative reward accumulated this episode (including per-step shaping).</summary>
        public float EpisodeReward => _episodeReward;

        /// <summary>Number of decision steps taken this episode.</summary>
        public int EpisodeStep => _episodeStep;

        /// <summary>Most recent discrete action values, indexed by branch.</summary>
        public int[] LastDiscreteActions { get; private set; }

        /// <summary>Most recent continuous action values.</summary>
        public float[] LastContinuousActions { get; private set; }
    }
}
