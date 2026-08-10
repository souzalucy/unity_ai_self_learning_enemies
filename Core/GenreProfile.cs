using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Supported game genres for auto-configuration.
    /// </summary>
    public enum EnemyGenre
    {
        RPG,
        Shooter,
        Racing,
        Custom
    }

    /// <summary>
    /// ScriptableObject that holds all genre-specific default settings.
    /// Drag a profile into EnemyBrain to auto-configure observations, actions, and rewards.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGenreProfile", menuName = "Self-Learning Enemies/Genre Profile")]
    public class GenreProfile : ScriptableObject
    {
        [Header("Genre Identity")]
        [Tooltip("The game style this profile targets.")]
        public EnemyGenre genre = EnemyGenre.Custom;

        [Tooltip("Human-readable description of this profile.")]
        [TextArea(2, 4)]
        public string description = "Describe the enemy behaviour expected from this profile.";

        [Header("Training Hyperparameters")]
        [Tooltip("Path to the YAML training config (relative to project root).")]
        public string trainingConfigPath = "Assets/SelfLearningEnemies/Training/";

        [Tooltip("Maximum number of steps per training episode.")]
        public int maxStep = 5000;

        [Tooltip("Whether to use curiosity-driven exploration (ICM) for sparse rewards.")]
        public bool useCuriosity = false;

        [Header("Action Space Presets")]
        [Tooltip("Number of discrete action branches.")]
        public int discreteBranchCount = 1;

        [Tooltip("Size of each discrete branch.")]
        public int[] discreteBranchSizes = new int[] { 4 };

        [Tooltip("Number of continuous action floats.")]
        public int continuousActionCount = 2;

        [Header("Observation Space Presets")]
        [Tooltip("Total observation vector size (sum of all ObservationSources).")]
        public int totalObservationSize = 20;

        [Tooltip("Enable stacked observations (frames of history).")]
        public int observationStackCount = 1;

        [Header("Reward Guidance")]
        [Tooltip("Base reward per second of survival.")]
        public float survivalRewardPerSecond = 0.01f;

        [Tooltip("Penalty on death / episode failure.")]
        public float deathPenalty = -1.0f;

        [Tooltip("Reward for completing the objective (kill, lap, etc.).")]
        public float objectiveCompleteReward = 1.0f;

        // ------------------------------------------------------------------
        // Factory helpers — used by editor scripts to create default profiles
        // ------------------------------------------------------------------

        public static GenreProfile CreateRPGDefaults()
        {
            var p = CreateInstance<GenreProfile>();
            p.genre = EnemyGenre.RPG;
            p.description = "RPG combat enemy: manages health, cooldowns, positioning, and ability selection.";
            p.trainingConfigPath = "Assets/SelfLearningEnemies/Training/rpg_trainer_config.yaml";
            p.maxStep = 3000;
            p.useCuriosity = false;
            p.discreteBranchCount = 1;
            p.discreteBranchSizes = new int[] { 6 }; // none, attack1, attack2, defend, heal, flee
            p.continuousActionCount = 2;              // move_x, move_z
            p.totalObservationSize = 28;
            p.observationStackCount = 3;
            p.survivalRewardPerSecond = 0.005f;
            p.deathPenalty = -1.0f;
            p.objectiveCompleteReward = 2.0f;
            return p;
        }

        public static GenreProfile CreateShooterDefaults()
        {
            var p = CreateInstance<GenreProfile>();
            p.genre = EnemyGenre.Shooter;
            p.description = "Cover-based shooter enemy: values positioning, ammo management, and flanking.";
            p.trainingConfigPath = "Assets/SelfLearningEnemies/Training/shooter_trainer_config.yaml";
            p.maxStep = 5000;
            p.useCuriosity = true;
            p.discreteBranchCount = 2;
            p.discreteBranchSizes = new int[] { 5, 4 }; // branch0: none/shoot/reload/grenade/melee, branch1: none/cover/flank/retreat
            p.continuousActionCount = 4;                // move_x, move_z, aim_yaw, aim_pitch
            p.totalObservationSize = 48;
            p.observationStackCount = 4;
            p.survivalRewardPerSecond = 0.002f;
            p.deathPenalty = -1.0f;
            p.objectiveCompleteReward = 3.0f;
            return p;
        }

        public static GenreProfile CreateRacingDefaults()
        {
            var p = CreateInstance<GenreProfile>();
            p.genre = EnemyGenre.Racing;
            p.description = "Racing opponent: optimizes line, speed, boost usage, and overtaking.";
            p.trainingConfigPath = "Assets/SelfLearningEnemies/Training/racing_trainer_config.yaml";
            p.maxStep = 10000;
            p.useCuriosity = false;
            p.discreteBranchCount = 2;
            p.discreteBranchSizes = new int[] { 3, 3 }; // branch0: none/boost/use_item, branch1: none/draft/block
            p.continuousActionCount = 3;                // steer, accelerate, brake
            p.totalObservationSize = 36;
            p.observationStackCount = 2;
            p.survivalRewardPerSecond = 0.0f;
            p.deathPenalty = 0.0f;
            p.objectiveCompleteReward = 1.0f;
            return p;
        }
    }
}
