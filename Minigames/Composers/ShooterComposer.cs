using UnityEngine;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>Shooter genre component stack: aim-and-shoot with raycast perception + cover rewards.</summary>
    public static class ShooterComposer
    {
        public static void AddObservations(GameObject go)
        {
            // 8 + 5 + 35 (7 rays) = 48
            ComposerUtils.Ensure<ObsTargetTransform>(go).maxDistance = 50f;
            ComposerUtils.Ensure<ObsSelfStatus>(go);
            var rays = ComposerUtils.Ensure<ObsRaycastPerception>(go);
            rays.numRays = 7;
            rays.fieldOfView = 120f;
            rays.maxDistance = 40f;
            rays.encodedTags = new string[] { "Player", "Enemy", "Wall", "Cover" };
        }

        public static void AddActions(GameObject go)
        {
            // 4 continuous (move x/z + aim yaw/pitch) + discrete [5, 4]
            ComposerUtils.Ensure<ActionNavMeshMovement>(go).moveSpeed = 5f;
            var aim = ComposerUtils.Ensure<ActionAimAndShoot>(go);
            aim.combatSlotCount = 4; // none, shoot, reload, grenade, melee
            if (aim.aimPivot == null) aim.aimPivot = CreateAimPivot(go);
            var tactics = ComposerUtils.Ensure<ActionItemUsage>(go);
            tactics.slotCount = 3; // none, cover, flank, retreat
        }

        public static void AddRewards(GameObject go)
        {
            ComposerUtils.Ensure<RewardCombatPerformance>(go);
            var survival = ComposerUtils.Ensure<RewardSurvival>(go);
            survival.survivalRate = 0.002f;
            ComposerUtils.Ensure<RewardCoverUsage>(go);
        }

        private static Transform CreateAimPivot(GameObject go)
        {
            // Give the aim its own pivot so it doesn't fight the NavMeshAgent's body rotation.
            var pivotGo = new GameObject("AimPivot");
            pivotGo.transform.SetParent(go.transform, false);
            pivotGo.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            return pivotGo.transform;
        }
    }
}
