using System.Collections.Generic;
using UnityEngine;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>
    /// Owns the lap/checkpoint bookkeeping for the Racing minigame, extracted from
    /// MinigameManager so the manager stays a thin coordinator. Pure state + logic:
    /// no MonoBehaviour lifecycle of its own.
    /// </summary>
    public class RacingRoundController
    {
        private readonly Dictionary<EntityId, int> _lastCheckpointByInstance = new Dictionary<EntityId, int>();
        private readonly Dictionary<EntityId, int> _lapByInstance = new Dictionary<EntityId, int>();
        private int _waypointCount;

        /// <summary>Wire checkpoint triggers onto every track waypoint and remember the count.</summary>
        public void ConfigureCheckpoints(RacingTrackBuilder track)
        {
            _waypointCount = track.Waypoints.Count;
            for (int i = 0; i < track.Waypoints.Count; i++)
            {
                if (track.Waypoints[i] == null) continue;
                EnsureCheckpoint(track.Waypoints[i], i);
            }
        }

        /// <summary>Hand each enemy the track waypoints for its waypoint observation/reward.</summary>
        public void AssignWaypoints(IReadOnlyList<EnemyBrain> enemies, List<Transform> waypoints)
        {
            foreach (var brain in enemies)
            {
                if (brain == null) continue;
                var obs = brain.GetComponent<ObsWaypointProgress>();
                if (obs != null) obs.waypoints = waypoints.ToArray();
                var rwd = brain.GetComponent<RewardWaypointProgress>();
                if (rwd != null) rwd.waypoints = waypoints.ToArray();
            }
        }

        /// <summary>Clear all lap/checkpoint state for a new round.</summary>
        public void Reset()
        {
            _lastCheckpointByInstance.Clear();
            _lapByInstance.Clear();
        }

        /// <summary>
        /// Record a checkpoint crossing. Returns true (with playerWon set) when the
        /// race is complete for this vehicle.
        /// </summary>
        public bool OnCheckpointPassed(GameObject vehicle, int waypointIndex, int lapsToWin, bool isPlayerVehicle, out bool playerWon)
        {
            EntityId id = vehicle.GetEntityId();
            if (!_lastCheckpointByInstance.TryGetValue(id, out int last)) last = -1;
            if (!_lapByInstance.TryGetValue(id, out int laps)) laps = 0;

            if (waypointIndex == 0 && last >= _waypointCount - 1) laps++;

            _lastCheckpointByInstance[id] = waypointIndex;
            _lapByInstance[id] = laps;

            playerWon = isPlayerVehicle && laps >= lapsToWin;
            return laps >= lapsToWin;
        }

        /// <summary>Current lap count for a vehicle (HUD / diagnostics).</summary>
        public int GetLaps(GameObject vehicle) =>
            vehicle != null && _lapByInstance.TryGetValue(vehicle.GetEntityId(), out int laps) ? laps : 0;

        /// <summary>Lap count for the human player.</summary>
        public int PlayerLaps(PlayerStatus player) => GetLaps(player != null ? player.gameObject : null);

        /// <summary>Best lap count across all enemy agents.</summary>
        public int BestEnemyLaps(IReadOnlyList<EnemyBrain> enemies)
        {
            int best = 0;
            foreach (var e in enemies)
                if (e != null) best = Mathf.Max(best, GetLaps(e.gameObject));
            return best;
        }

        private static void EnsureCheckpoint(Transform waypoint, int index)
        {
            var cp = waypoint.GetComponent<TrackCheckpoint>();
            if (cp == null) cp = waypoint.gameObject.AddComponent<TrackCheckpoint>();
            cp.waypointIndex = index;

            var col = waypoint.GetComponent<Collider>();
            if (col == null)
            {
                var box = waypoint.gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(8f, 2f, 2f);
            }
            else
            {
                col.isTrigger = true;
            }
        }
    }
}
