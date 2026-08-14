using Unity.MLAgents.Sensors;
using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// For racing / navigation: observes the next N waypoints relative to the enemy.
    /// Each waypoint contributes 3 floats: relativePos normalized.
    /// Total size = numWaypoints * 3.
    /// </summary>
    public class ObsWaypointProgress : ObservationSource
    {
        [Tooltip("Array of waypoint transforms in track order.")]
        public Transform[] waypoints;

        [Tooltip("How many upcoming waypoints to observe.")]
        [Range(1, 10)]
        public int numWaypoints = 3;

        [Tooltip("Normalization distance for waypoint offsets.")]
        public float normalizeDistance = 50f;

        [Tooltip("Current waypoint index (set externally by track manager).")]
        public int currentWaypointIndex = 0;

        public override int ObservationSize => numWaypoints * 3;

        public override void CollectObservations(VectorSensor sensor)
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                WriteEmptyWaypoints(sensor);
                return;
            }

            for (int i = 0; i < numWaypoints; i++)
                WriteWaypoint(sensor, i);
        }

        private void WriteEmptyWaypoints(VectorSensor sensor)
        {
            for (int i = 0; i < numWaypoints * 3; i++)
                sensor.AddObservation(0f);
        }

        private void WriteWaypoint(VectorSensor sensor, int i)
        {
            int idx = (currentWaypointIndex + i) % waypoints.Length;
            Transform wp = waypoints[idx];

            if (wp == null)
            {
                sensor.AddObservation(0f); sensor.AddObservation(0f); sensor.AddObservation(0f);
                return;
            }

            Vector3 delta = wp.position - transform.position;
            sensor.AddObservation(Mathf.Clamp(delta.x / normalizeDistance, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(delta.y / normalizeDistance, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(delta.z / normalizeDistance, -1f, 1f));
        }
    }
}
