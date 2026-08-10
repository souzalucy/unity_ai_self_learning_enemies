using System.Collections.Generic;
using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Racing-style training track with waypoints, start/finish line, and oval layout.
    /// Right-click → Build Arena to generate.
    /// </summary>
    public class RacingTrackBuilder : TrainingArenaBuilder
    {
        [Header("Track")]
        public float trackWidth = 8f;
        public int waypointCount = 20;
        public float cornerRadius = 15f;
        public float straightLength = 30f;
        public Material trackMaterial;
        public bool showWaypoints = true;

        public List<Transform> Waypoints { get; private set; } = new List<Transform>();

        protected override void BuildGenreFeatures()
        {
            var trackRoot = new GameObject("Track");
            trackRoot.transform.SetParent(_arenaRoot.transform);
            BuildTrackSurface(trackRoot.transform);
            BuildWaypoints(trackRoot.transform);
            BuildStartFinish(trackRoot.transform);
        }

        private void BuildTrackSurface(Transform parent)
        {
            int segments = 40;
            var mesh = new GameObject("TrackSurface"); mesh.transform.SetParent(parent);
            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / segments;
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = $"Seg_{i}"; seg.transform.SetParent(mesh.transform);
                seg.transform.position = GetTrackPos(t);
                seg.transform.rotation = Quaternion.LookRotation(GetTrackDir(t), Vector3.up);
                float totalLen = straightLength * 2 + Mathf.PI * cornerRadius * 2;
                seg.transform.localScale = new Vector3(trackWidth, 0.1f, totalLen / segments);
                if (trackMaterial != null) seg.GetComponent<Renderer>().material = trackMaterial;
            }
        }

        private Vector3 GetTrackPos(float t)
        {
            t = Mathf.Repeat(t, 1f);
            float totalLen = straightLength * 2 + Mathf.PI * cornerRadius * 2;
            float topEnd = straightLength / totalLen;
            float rightEnd = topEnd + Mathf.PI * cornerRadius / totalLen;
            float bottomEnd = rightEnd + straightLength / totalLen;
            float hs = straightLength * 0.5f;

            if (t < topEnd)
                return new Vector3(Mathf.Lerp(-hs, hs, t / topEnd), 0, cornerRadius);
            if (t < rightEnd)
            {
                float a = Mathf.Lerp(0, Mathf.PI, (t - topEnd) / (rightEnd - topEnd));
                return new Vector3(hs + Mathf.Sin(a) * cornerRadius, 0, Mathf.Cos(a) * cornerRadius);
            }
            if (t < bottomEnd)
                return new Vector3(Mathf.Lerp(hs, -hs, (t - rightEnd) / (bottomEnd - rightEnd)), 0, -cornerRadius);

            float a2 = Mathf.Lerp(Mathf.PI, Mathf.PI * 2, (t - bottomEnd) / (1f - bottomEnd));
            return new Vector3(-hs + Mathf.Sin(a2) * cornerRadius, 0, Mathf.Cos(a2) * cornerRadius);
        }


        private Vector3 GetTrackDir(float t)
        { float e = 0.001f; return (GetTrackPos(t + e) - GetTrackPos(t - e)).normalized; }

        private void BuildWaypoints(Transform parent)
        {
            var wpRoot = new GameObject("Waypoints"); wpRoot.transform.SetParent(parent);
            for (int i = 0; i < waypointCount; i++)
            {
                var wp = new GameObject($"WP_{i}"); wp.transform.SetParent(wpRoot.transform);
                wp.transform.position = GetTrackPos((float)i / waypointCount) + Vector3.up * 0.5f;
                wp.tag = "Waypoint"; Waypoints.Add(wp.transform);
            }
        }

        private void BuildStartFinish(Transform parent)
        {
            var sf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sf.name = "StartFinish"; sf.transform.SetParent(parent);
            sf.transform.position = GetTrackPos(0f) + Vector3.up * 2f;
            sf.transform.localScale = new Vector3(trackWidth, 0.2f, 1f);
            if (trackMaterial != null) sf.GetComponent<Renderer>().material = trackMaterial;
        }

        protected override void CreateSpawnPoints()
        {
            _spawnPoints = new Transform[enemyCount];
            if (Waypoints.Count == 0) return;
            var root = new GameObject("SpawnPoints"); root.transform.SetParent(_arenaRoot.transform);
            for (int i = 0; i < enemyCount; i++)
            {
                int idx = (i * waypointCount / (enemyCount + 1)) % Waypoints.Count;
                var sp = new GameObject($"Spawn_{i}"); sp.transform.SetParent(root.transform);
                sp.transform.position = Waypoints[idx].position + Vector3.right * (i - enemyCount / 2) * 2f;
                _spawnPoints[i] = sp.transform;
            }
        }

        private void OnDrawGizmos()
        {
            if (!showWaypoints || Waypoints == null) return;
            Gizmos.color = Color.yellow;
            for (int i = 0; i < Waypoints.Count; i++)
            {
                if (Waypoints[i] == null) continue;
                Gizmos.DrawSphere(Waypoints[i].position, 0.3f);
                int next = (i + 1) % Waypoints.Count;
                if (Waypoints[next] != null) Gizmos.DrawLine(Waypoints[i].position, Waypoints[next].position);
            }
        }
    }
}
