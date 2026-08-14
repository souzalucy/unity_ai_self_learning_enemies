using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.AI;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>
    /// Programmatically composes a bare GameObject into a fully-wired player or enemy for a
    /// given genre. This is the "integration glue" that turns the component library into a
    /// playable experiment without hand-wiring prefabs in the inspector. It is idempotent:
    /// calling it on a GameObject that already has some components only fills in the gaps.
    ///
    /// Genre-specific component stacks live in the dedicated composer classes
    /// (RpgComposer / ShooterComposer / RacingComposer); this class keeps only the
    /// public entry points and the orchestration.
    /// </summary>
    public static class MinigameComposer
    {
        /// <summary>Ensures a GameObject has the components needed to act as the human player.</summary>
        public static void ConfigurePlayer(GameObject go, MinigameSettings settings)
        {
            if (settings == null || go == null) return;
            go.tag = "Player";

            var status = ComposerUtils.Ensure<SimpleStatusProvider>(go);
            status.Configure(settings.playerMaxHealth, 100f, 0f);
            ComposerUtils.Ensure<StatusDamageReceiver>(go);
            ComposerUtils.Ensure<PlayerStatus>(go);

            if (settings.genre == EnemyGenre.Racing)
            {
                ComposerUtils.Ensure<Rigidbody>(go);
                ComposerUtils.Ensure<PlayerCarController>(go);
            }
            else
            {
                ComposerUtils.Ensure<NavMeshAgent>(go);
                ComposerUtils.Ensure<PlayerController>(go);
            }
        }

        /// <summary>
        /// Ensures a GameObject has the components needed to act as an enemy agent.
        /// Returns the EnemyBrain component for further configuration.
        /// </summary>
        public static EnemyBrain ConfigureEnemy(GameObject go, MinigameSettings settings, Transform player)
        {
            if (settings == null || go == null) return null;
            go.tag = "Enemy";

            ConfigureStatus(go, settings);
            ConfigureTargeting(go);
            AddObservations(go, settings.genre);
            AddActions(go, settings.genre);
            AddRewards(go, settings.genre);
            ConfigureLearning(go, settings, player);
            BehaviorConfigurator.Configure(go, settings);

            return go.GetComponent<EnemyBrain>();
        }

        private static void ConfigureStatus(GameObject go, MinigameSettings settings)
        {
            float hpMult = settings.enemyHealthMultiplier > 0.001f ? settings.enemyHealthMultiplier : 1f;
            var status = ComposerUtils.Ensure<SimpleStatusProvider>(go);
            status.Configure(100f * hpMult, 100f, 0f);
            ComposerUtils.Ensure<StatusDamageReceiver>(go);
        }

        private static void ConfigureTargeting(GameObject go)
        {
            var target = ComposerUtils.Ensure<SimpleTargetProvider>(go);
            target.targetTag = "Player";
        }

        private static void ConfigureLearning(GameObject go, MinigameSettings settings, Transform player)
        {
            // BehaviorParameters + DecisionRequester must exist before EnemyBrain so its
            // [RequireComponent] attributes don't re-add them with defaults.
            ComposerUtils.Ensure<BehaviorParameters>(go);
            var requester = ComposerUtils.Ensure<DecisionRequester>(go);
            requester.DecisionPeriod = 5;
            requester.TakeActionsBetweenDecisions = true;

            var brain = ComposerUtils.Ensure<EnemyBrain>(go);
            brain.genreProfile = settings.ResolveProfile();

            ComposerUtils.Ensure<EnemyWiring>(go);
            WireCombatTarget(go, player);
        }

        private static void WireCombatTarget(GameObject go, Transform player)
        {
            if (player == null) return;
            var combat = go.GetComponent<ActionCombat>();
            if (combat != null) combat.attackTarget = player;
        }

        private static void AddObservations(GameObject go, EnemyGenre genre)
        {
            switch (genre)
            {
                case EnemyGenre.RPG: RpgComposer.AddObservations(go); break;
                case EnemyGenre.Shooter: ShooterComposer.AddObservations(go); break;
                case EnemyGenre.Racing: RacingComposer.AddObservations(go); break;
            }
        }

        private static void AddActions(GameObject go, EnemyGenre genre)
        {
            switch (genre)
            {
                case EnemyGenre.RPG: RpgComposer.AddActions(go); break;
                case EnemyGenre.Shooter: ShooterComposer.AddActions(go); break;
                case EnemyGenre.Racing: RacingComposer.AddActions(go); break;
            }
        }

        private static void AddRewards(GameObject go, EnemyGenre genre)
        {
            switch (genre)
            {
                case EnemyGenre.RPG: RpgComposer.AddRewards(go); break;
                case EnemyGenre.Shooter: ShooterComposer.AddRewards(go); break;
                case EnemyGenre.Racing: RacingComposer.AddRewards(go); break;
            }
        }
    }
}
