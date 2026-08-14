using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>
    /// How the enemy should behave in a minigame. This is the "experiment switch"
    /// exposed per-genre: drive it yourself, train it live, or run a trained model.
    /// </summary>
    public enum ExperimentMode
    {
        /// <summary>Drive an enemy manually with WASD/Space/E/Q (uses EnemyBrain.Heuristic).</summary>
        HeuristicOnly,

        /// <summary>Run <c>mlagents-learn</c> and let the policy learn live.</summary>
        Training,

        /// <summary>Load a trained .onnx model and run inference only.</summary>
        InferenceOnly
    }

    /// <summary>
    /// ScriptableObject that configures a single playable minigame. One scene can serve
    /// as both a training sandbox and a playable demo by switching <see cref="experimentMode"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMinigameSettings", menuName = "Self-Learning Enemies/Minigame Settings")]
    public class MinigameSettings : ScriptableObject
    {
        [Header("Genre")]
        [Tooltip("Which genre this minigame implements.")]
        public EnemyGenre genre = EnemyGenre.RPG;

        [Tooltip("Optional: genre profile used for reward defaults. If null, the factory defaults are used.")]
        public GenreProfile genreProfile;

        [Header("Experiment")]
        [Tooltip("How the enemy behaves: manual control, live training, or a trained model.")]
        public ExperimentMode experimentMode = ExperimentMode.HeuristicOnly;

        [Tooltip("Trained model used when experimentMode = InferenceOnly.")]
        public NNModel inferenceModel;

        [Tooltip("Global time scale applied to the minigame.")]
        [Range(0.1f, 10f)]
        public float timeScale = 1f;

        [Header("Game Loop")]
        [Tooltip("Number of enemy agents to spawn.")]
        [Min(1)]
        public int enemyCount = 1;

        [Tooltip("Time limit in seconds. 0 = unlimited.")]
        public float timeLimitSeconds = 120f;

        [Tooltip("RPG: how many waves the player must survive to win.")]
        [Min(1)]
        public int wavesToSurvive = 3;

        [Tooltip("Racing: how many laps complete the race.")]
        [Min(1)]
        public int lapsToWin = 3;

        [Tooltip("Delay (seconds) between a round ending and the reset.")]
        public float roundResetDelay = 1.5f;

        [Header("Difficulty")]
        [Tooltip("Multiplies enemy max health.")]
        public float enemyHealthMultiplier = 1f;

        [Tooltip("Multiplies enemy damage.")]
        public float enemyDamageMultiplier = 1f;

        [Tooltip("Player max health.")]
        public float playerMaxHealth = 100f;

        [Header("Prefabs")]
        [Tooltip("Player prefab. If null, a default is composed at runtime.")]
        public GameObject playerPrefab;

        [Tooltip("Enemy prefab. If null, a default is composed at runtime.")]
        public GameObject enemyPrefab;

        private GenreProfile _cachedFallbackProfile;

        /// <summary>
        /// Returns the assigned profile, or a cached factory-default profile for the genre.
        /// </summary>
        public GenreProfile ResolveProfile()
        {
            if (genreProfile != null) return genreProfile;

            if (_cachedFallbackProfile == null)
            {
                _cachedFallbackProfile = genre switch
                {
                    EnemyGenre.RPG => GenreProfile.CreateRPGDefaults(),
                    EnemyGenre.Shooter => GenreProfile.CreateShooterDefaults(),
                    EnemyGenre.Racing => GenreProfile.CreateRacingDefaults(),
                    _ => CreateInstance<GenreProfile>()
                };
            }
            return _cachedFallbackProfile;
        }
    }
}
