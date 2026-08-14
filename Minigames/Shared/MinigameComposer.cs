using System.Collections.Generic;
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
    /// </summary>
    public static class MinigameComposer
    {
        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>
        /// Ensures a GameObject has the components needed to act as the human player.
        /// </summary>
        public static void ConfigurePlayer(GameObject go, MinigameSettings settings)
        {
            if (settings == null || go == null) return;
            go.tag = "Player";

            var status = Ensure<SimpleStatusProvider>(go);
            status.Configure(settings.playerMaxHealth, 100f, 0f);

            Ensure<StatusDamageReceiver>(go);
            Ensure<PlayerStatus>(go);

            if (settings.genre == EnemyGenre.Racing)
            {
                Ensure<Rigidbody>(go);
                Ensure<PlayerCarController>(go);
            }
            else
            {
                Ensure<NavMeshAgent>(go);
                Ensure<PlayerController>(go);
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

            float hpMult = settings.enemyHealthMultiplier > 0.001f ? settings.enemyHealthMultiplier : 1f;
            var status = Ensure<SimpleStatusProvider>(go);
            status.Configure(100f * hpMult, 100f, 0f);

            Ensure<StatusDamageReceiver>(go);

            var target = Ensure<SimpleTargetProvider>(go);
            target.targetTag = "Player";

            AddObservations(go, settings.genre);
            AddActions(go, settings.genre);
            AddRewards(go, settings.genre);

            // BehaviorParameters + DecisionRequester must exist before EnemyBrain so its
            // [RequireComponent] attributes don't re-add them with defaults.
            Ensure<BehaviorParameters>(go);
            var requester = Ensure<DecisionRequester>(go);
            requester.DecisionPeriod = 5;
            requester.TakeActionsBetweenDecisions = true;

            var brain = Ensure<EnemyBrain>(go);
            brain.genreProfile = settings.ResolveProfile();

            Ensure<EnemyWiring>(go);

            if (player != null)
            {
                var combat = go.GetComponent<ActionCombat>();
                if (combat != null) combat.attackTarget = player;
            }

            ConfigureBehaviorParameters(go, settings);
            return brain;
        }

        // ------------------------------------------------------------------
        // Genre composition
        // ------------------------------------------------------------------

        private static void AddObservations(GameObject go, EnemyGenre genre)
        {
            switch (genre)
            {
                case EnemyGenre.RPG:
                    // 7 + 8 + 5 + 8 (grid 2x2 x2 tags) = 28
                    Ensure<ObsSelfTransform>(go).arenaSize = new Vector3(50f, 0f, 50f);
                    Ensure<ObsTargetTransform>(go).maxDistance = 50f;
                    Ensure<ObsSelfStatus>(go);
                    var grid = Ensure<ObsGridSensor>(go);
                    grid.gridSizeX = 2;
                    grid.gridSizeZ = 2;
                    grid.encodedTags = new string[] { "Player", "Cover" };
                    break;

                case EnemyGenre.Shooter:
                    // 8 + 5 + 35 (7 rays) = 48
                    Ensure<ObsTargetTransform>(go).maxDistance = 50f;
                    Ensure<ObsSelfStatus>(go);
                    var rays = Ensure<ObsRaycastPerception>(go);
                    rays.numRays = 7;
                    rays.fieldOfView = 120f;
                    rays.maxDistance = 40f;
                    rays.encodedTags = new string[] { "Player", "Enemy", "Wall", "Cover" };
                    break;

                case EnemyGenre.Racing:
                    // 7 + 9 (3 waypoints) + 20 (4 rays) = 36
                    Ensure<ObsSelfTransform>(go).arenaSize = new Vector3(100f, 0f, 100f);
                    var wp = Ensure<ObsWaypointProgress>(go);
                    wp.numWaypoints = 3;
                    var ray = Ensure<ObsRaycastPerception>(go);
                    ray.numRays = 4;
                    ray.fieldOfView = 60f;
                    ray.maxDistance = 20f;
                    ray.eyeHeight = 1f;
                    break;
            }
        }

        private static void AddActions(GameObject go, EnemyGenre genre)
        {
            switch (genre)
            {
                case EnemyGenre.RPG:
                    // 2 continuous (move) + 1 discrete branch [6]
                    Ensure<ActionNavMeshMovement>(go).moveSpeed = 4f;
                    var combat = Ensure<ActionCombat>(go);
                    combat.actionSlotCount = 5; // none, attack1, attack2, defend, heal, flee
                    combat.damageValues = new float[] { 20f, 40f, 0f, 0f, 0f };
                    combat.cooldowns = new float[] { 0.6f, 1.5f, 0.5f, 4f, 1f };
                    break;

                case EnemyGenre.Shooter:
                    // 4 continuous (move x/z + aim yaw/pitch) + discrete [5, 4]
                    Ensure<ActionNavMeshMovement>(go).moveSpeed = 5f;
                    var aim = Ensure<ActionAimAndShoot>(go);
                    aim.combatSlotCount = 4; // none, shoot, reload, grenade, melee
                    if (aim.aimPivot == null)
                    {
                        // Give the aim its own pivot so it doesn't fight the NavMeshAgent's
                        // body rotation.
                        var pivotGo = new GameObject("AimPivot");
                        pivotGo.transform.SetParent(go.transform, false);
                        pivotGo.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                        aim.aimPivot = pivotGo.transform;
                    }
                    var tactics = Ensure<ActionItemUsage>(go);
                    tactics.slotCount = 3; // none, cover, flank, retreat
                    break;

                case EnemyGenre.Racing:
                    // 3 continuous (steer/accel/brake) + discrete [3, 3]
                    Ensure<ActionRigidBodyMovement>(go);
                    var items = go.GetComponents<ActionItemUsage>();
                    if (items.Length == 0)
                    {
                        go.AddComponent<ActionItemUsage>().slotCount = 2; // none, boost, use_item
                        go.AddComponent<ActionItemUsage>().slotCount = 2; // none, draft, block
                    }
                    else if (items.Length == 1)
                    {
                        items[0].slotCount = 2;
                        go.AddComponent<ActionItemUsage>().slotCount = 2;
                    }
                    else
                    {
                        for (int i = 0; i < items.Length; i++) items[i].slotCount = 2;
                    }
                    break;
            }
        }

        private static void AddRewards(GameObject go, EnemyGenre genre)
        {
            switch (genre)
            {
                case EnemyGenre.RPG:
                    Ensure<RewardCombatPerformance>(go);
                    var survival = Ensure<RewardSurvival>(go);
                    survival.survivalRate = 0.005f;
                    var distance = Ensure<RewardDistanceManagement>(go);
                    distance.preferredDistance = 2f; // melee gap-closing
                    distance.maxDistance = 15f;
                    distance.sigma = 2f;
                    break;

                case EnemyGenre.Shooter:
                    Ensure<RewardCombatPerformance>(go);
                    var s = Ensure<RewardSurvival>(go);
                    s.survivalRate = 0.002f;
                    Ensure<RewardCoverUsage>(go);
                    break;

                case EnemyGenre.Racing:
                    Ensure<RewardWaypointProgress>(go);
                    break;
            }
        }

        private static void ConfigureBehaviorParameters(GameObject go, MinigameSettings settings)
        {
            var bp = go.GetComponent<BehaviorParameters>();
            if (bp == null) return;

            int obsSize = 0;
            foreach (var o in go.GetComponents<ObservationSource>())
                if (o != null) obsSize += o.ObservationSize;

            var branchSizes = new List<int>();
            int contTotal = 0;
            foreach (var a in go.GetComponents<ActionEffect>())
            {
                if (a == null) continue;
                if (a.DiscreteBranchSizes != null) branchSizes.AddRange(a.DiscreteBranchSizes);
                contTotal += a.ContinuousActionCount;
            }

            // Mirrors EnemyBrainEditor.AutoConfigureBP(), but at runtime.
            var brainParameters = bp.BrainParameters;
            brainParameters.VectorObservationSize = obsSize;
            var actionSpec = brainParameters.ActionSpec;
            actionSpec.NumContinuousActions = contTotal;
            actionSpec.BranchSizes = branchSizes.ToArray();
            brainParameters.ActionSpec = actionSpec;

            switch (settings.experimentMode)
            {
                case ExperimentMode.HeuristicOnly:
                    bp.BehaviorType = BehaviorType.HeuristicOnly;
                    break;
                case ExperimentMode.Training:
                    bp.BehaviorType = BehaviorType.Default;
                    break;
                case ExperimentMode.InferenceOnly:
                    bp.BehaviorType = BehaviorType.InferenceOnly;
                    if (settings.inferenceModel != null) bp.Model = settings.inferenceModel;
                    else Debug.LogWarning("[MinigameComposer] InferenceOnly selected but no model assigned.");
                    break;
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static T Ensure<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }
}


