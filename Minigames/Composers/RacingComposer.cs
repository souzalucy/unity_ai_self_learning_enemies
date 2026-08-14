using UnityEngine;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>Racing genre component stack: rigidbody driving with waypoint progress rewards.</summary>
    public static class RacingComposer
    {
        public static void AddObservations(GameObject go)
        {
            // 7 + 9 (3 waypoints) + 20 (4 rays) = 36
            ComposerUtils.Ensure<ObsSelfTransform>(go).arenaSize = new Vector3(100f, 0f, 100f);
            var wp = ComposerUtils.Ensure<ObsWaypointProgress>(go);
            wp.numWaypoints = 3;
            var ray = ComposerUtils.Ensure<ObsRaycastPerception>(go);
            ray.numRays = 4;
            ray.fieldOfView = 60f;
            ray.maxDistance = 20f;
            ray.eyeHeight = 1f;
        }

        public static void AddActions(GameObject go)
        {
            // 3 continuous (steer/accel/brake) + discrete [3, 3]
            ComposerUtils.Ensure<ActionRigidBodyMovement>(go);
            ConfigureRacingItems(go);
        }

        public static void AddRewards(GameObject go)
        {
            ComposerUtils.Ensure<RewardWaypointProgress>(go);
        }

        private static void ConfigureRacingItems(GameObject go)
        {
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
        }
    }
}
