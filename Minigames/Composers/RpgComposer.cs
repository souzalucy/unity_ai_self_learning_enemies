using UnityEngine;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>RPG genre component stack: melee NavMesh agent with combat + survival rewards.</summary>
    public static class RpgComposer
    {
        public static void AddObservations(GameObject go)
        {
            // 7 + 8 + 5 + 8 (grid 2x2 x2 tags) = 28
            ComposerUtils.Ensure<ObsSelfTransform>(go).arenaSize = new Vector3(50f, 0f, 50f);
            ComposerUtils.Ensure<ObsTargetTransform>(go).maxDistance = 50f;
            ComposerUtils.Ensure<ObsSelfStatus>(go);
            var grid = ComposerUtils.Ensure<ObsGridSensor>(go);
            grid.gridSizeX = 2;
            grid.gridSizeZ = 2;
            grid.encodedTags = new string[] { "Player", "Cover" };
        }

        public static void AddActions(GameObject go)
        {
            // 2 continuous (move) + 1 discrete branch [6]
            ComposerUtils.Ensure<ActionNavMeshMovement>(go).moveSpeed = 4f;
            var combat = ComposerUtils.Ensure<ActionCombat>(go);
            combat.actionSlotCount = 5; // none, attack1, attack2, defend, heal, flee
            combat.damageValues = new float[] { 20f, 40f, 0f, 0f, 0f };
            combat.cooldowns = new float[] { 0.6f, 1.5f, 0.5f, 4f, 1f };
        }

        public static void AddRewards(GameObject go)
        {
            ComposerUtils.Ensure<RewardCombatPerformance>(go);
            var survival = ComposerUtils.Ensure<RewardSurvival>(go);
            survival.survivalRate = 0.005f;
            var distance = ComposerUtils.Ensure<RewardDistanceManagement>(go);
            distance.preferredDistance = 2f; // melee gap-closing
            distance.maxDistance = 15f;
            distance.sigma = 2f;
        }
    }
}
