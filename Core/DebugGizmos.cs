using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SelfLearningEnemies
{
    /// <summary>
    /// All-in-one debug visualization. Add to EnemyBrain GameObject to see raycasts,
    /// cover, distance rings, waypoints, and reward values in the Scene view.
    /// </summary>
    [ExecuteAlways]
    public class DebugGizmos : MonoBehaviour
    {
        [Header("Toggles")]
        public bool showRaycasts = true;
        public bool showCoverDetection = true;
        public bool showDistanceRings = true;
        public bool showWaypoints = true;
        public bool showRewardHeatmap = true;

        [Header("Colors")]
        public Color rayColor = Color.cyan;
        public Color rayHitColor = Color.red;
        public Color coverColor = Color.green;
        public Color exposedColor = Color.red;
        public Color preferredRingColor = Color.yellow;
        public Color maxRingColor = new Color(1f, 0.5f, 0f, 0.5f);
        public Color waypointColor = Color.yellow;
        public float waypointSize = 0.3f;
        public float rewardTextSize = 0.15f;
        public Color positiveColor = Color.green;
        public Color negativeColor = Color.red;
        public Color neutralColor = Color.grey;

        private ObsRaycastPerception _rayObs;
        private RewardCoverUsage _cover;
        private RewardDistanceManagement _dist;
        private ObsWaypointProgress _wpObs;
        private RewardWaypointProgress _wpRwd;

        private void OnEnable() { Refresh(); }
        private void Refresh()
        {
            _rayObs = GetComponent<ObsRaycastPerception>();
            _cover = GetComponent<RewardCoverUsage>();
            _dist = GetComponent<RewardDistanceManagement>();
            _wpObs = GetComponent<ObsWaypointProgress>();
            _wpRwd = GetComponent<RewardWaypointProgress>();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!isActiveAndEnabled) return;
            Refresh();
            if (showRaycasts && _rayObs != null) DrawRays();
            if (showCoverDetection && _cover != null) DrawCover();
            if (showDistanceRings && _dist != null) DrawRings();
            if (showWaypoints) DrawWps();
            if (showRewardHeatmap) DrawRewards();
        }


        private void DrawRays()
        {
            var so = new SerializedObject(_rayObs);
            int n = so.FindProperty("numRays").intValue;
            float fov = so.FindProperty("fieldOfView").floatValue;
            float maxD = so.FindProperty("maxDistance").floatValue;
            float eh = so.FindProperty("eyeHeight").floatValue;
            int mask = so.FindProperty("raycastMask").intValue;
            Vector3 o = transform.position + Vector3.up * eh;
            float hf = fov * 0.5f;
            for (int i = 0; i < n; i++)
            {
                float a = n > 1 ? -hf + (fov / (n - 1)) * i : 0f;
                Vector3 d = Quaternion.Euler(0, a, 0) * transform.forward;
                bool hit = Physics.Raycast(o, d, out RaycastHit hi, maxD, mask);
                Gizmos.color = hit ? rayHitColor : rayColor;
                Gizmos.DrawLine(o, hit ? hi.point : o + d * maxD);
                if (hit) Gizmos.DrawSphere(hi.point, 0.1f);
            }
        }

        private void DrawCover()
        {
            var so = new SerializedObject(_cover);
            float maxD = so.FindProperty("maxThreatDistance").floatValue;
            string tag = so.FindProperty("coverTag").stringValue;
            int layers = so.FindProperty("coverLayers").intValue;
            Transform threat = so.FindProperty("threat")?.objectReferenceValue as Transform;
            if (threat == null) { var tp = GetComponent<ITargetProvider>(); if (tp != null && tp.HasValidTarget) threat = tp.Target; }
            if (threat == null) return;
            Vector3 dir = (threat.position - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, threat.position);
            if (dist > maxD) return;
            bool inC = Physics.Raycast(transform.position, dir, out RaycastHit h, dist, layers) && h.collider.CompareTag(tag);
            Gizmos.color = inC ? coverColor : exposedColor;
            Gizmos.DrawLine(transform.position, inC ? h.point : threat.position);
            if (inC) { Gizmos.DrawWireCube(h.point, Vector3.one * 0.3f); }
            Vector3 mid = (transform.position + (inC ? h.point : threat.position)) * 0.5f;
            Handles.Label(mid + Vector3.up * 0.5f, inC ? "COVER" : "EXPOSED",
                new GUIStyle { normal = { textColor = inC ? Color.green : Color.red } });
        }

        private void DrawRings()
        {
            var so = new SerializedObject(_dist);
            float pref = so.FindProperty("preferredDistance").floatValue;
            float maxD = so.FindProperty("maxDistance").floatValue;
            DrawCircle(transform.position, pref, preferredRingColor);
            DrawCircle(transform.position, maxD, maxRingColor);
            Handles.Label(transform.position + Vector3.right * pref + Vector3.up * 0.2f, "Preferred",
                new GUIStyle { normal = { textColor = preferredRingColor } });
        }

        private void DrawWps()
        {
            Transform[] wps = null;
            if (_wpObs != null) { var so = new SerializedObject(_wpObs); var a = so.FindProperty("waypoints"); if (a != null && a.isArray) { wps = new Transform[a.arraySize]; for (int i = 0; i < a.arraySize; i++) wps[i] = a.GetArrayElementAtIndex(i).objectReferenceValue as Transform; } }
            if (wps == null && _wpRwd != null) { var so = new SerializedObject(_wpRwd); var a = so.FindProperty("waypoints"); if (a != null && a.isArray) { wps = new Transform[a.arraySize]; for (int i = 0; i < a.arraySize; i++) wps[i] = a.GetArrayElementAtIndex(i).objectReferenceValue as Transform; } }
            if (wps == null || wps.Length == 0) return;
            Gizmos.color = waypointColor;
            for (int i = 0; i < wps.Length; i++)

        private void DrawRewards()
        {
            var sources = GetComponents<RewardSource>();
            if (sources == null || sources.Length == 0) return;
            float y = 2f, total = 0f;
            foreach (var r in sources)
            {
                if (r == null || !r.IsActive) continue;
                float v = r.CalculateReward() * r.RewardWeight; total += v;
                Color c = v > 0.01f ? positiveColor : v < -0.01f ? negativeColor : neutralColor;
                Gizmos.color = c; Gizmos.DrawSphere(transform.position + Vector3.up * y + Vector3.right * 0.5f, rewardTextSize);
                Handles.Label(transform.position + Vector3.up * y + Vector3.right * 0.8f, $"{r.SourceName}: {v:F3}", new GUIStyle { normal = { textColor = c } });
                y += 0.4f;
            }
            Color tc = total > 0.01f ? positiveColor : total < -0.01f ? negativeColor : neutralColor;
            Gizmos.color = tc; Gizmos.DrawSphere(transform.position + Vector3.up * y, rewardTextSize * 1.5f);
            Handles.Label(transform.position + Vector3.up * (y + 0.2f) + Vector3.right * 0.3f, $"TOTAL: {total:F3}", new GUIStyle { normal = { textColor = tc }, fontStyle = FontStyle.Bold });
        }

        private void DrawCircle(Vector3 c, float r, Color col)
        {
            Gizmos.color = col;
            int seg = 32; float step = 360f / seg;
            Vector3 prev = c + Vector3.right * r;
            for (int i = 1; i <= seg; i++)
            { float a = step * i * Mathf.Deg2Rad; Vector3 cur = c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); Gizmos.DrawLine(prev, cur); prev = cur; }
        }
#endif
    }
}

            { if (wps[i] == null) continue; Gizmos.DrawSphere(wps[i].position, waypointSize); int nx = (i + 1) % wps.Length; if (wps[nx] != null) Gizmos.DrawLine(wps[i].position, wps[nx].position); }
        }
