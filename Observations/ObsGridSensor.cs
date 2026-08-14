using Unity.MLAgents.Sensors;
using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Grid-based spatial observation. Divides the area around the enemy into an X×Z grid
    /// of cells and reports what occupies each cell as a one-hot tag encoding.
    /// Total size = gridX * gridZ * tagCount.
    /// </summary>
    public class ObsGridSensor : ObservationSource
    {
        [Header("Grid Dimensions")]
        [Tooltip("Number of cells along X (left/right).")]
        [Range(3, 32)]
        public int gridSizeX = 8;

        [Tooltip("Number of cells along Z (forward/backward).")]
        [Range(3, 32)]
        public int gridSizeZ = 8;

        [Tooltip("World size of each cell.")]
        public float cellSize = 2f;

        [Tooltip("Height of the detection box.")]
        public float detectionHeight = 3f;

        [Header("Detection")]
        [Tooltip("Layers to check for objects.")]
        public LayerMask detectionMask = -1;

        [Tooltip("Tags to encode as one-hot. Order matters — index 0 = first tag.")]
        public string[] encodedTags = new string[] { "Player", "Enemy", "Wall", "Obstacle", "Pickup" };

        [Tooltip("Center the grid on the enemy or offset forward (false = full surround).")]
        public bool centerOnEnemy = true;

        [Tooltip("Forward offset for the grid (applied when centerOnEnemy = false).")]
        public float forwardOffset = 0f;

        private Collider[] _results = new Collider[16];

        public override int ObservationSize => gridSizeX * gridSizeZ * encodedTags.Length;

        public override void CollectObservations(VectorSensor sensor)
        {
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            Vector3 gridOrigin = ComputeGridOrigin(origin, right, forward);
            Vector3 halfExtents = new Vector3(cellSize * 0.5f, detectionHeight * 0.5f, cellSize * 0.5f);

            for (int x = 0; x < gridSizeX; x++)
                for (int z = 0; z < gridSizeZ; z++)
                    WriteCell(sensor, gridOrigin + right * (x * cellSize) + forward * (z * cellSize), halfExtents);
        }

        private Vector3 ComputeGridOrigin(Vector3 origin, Vector3 right, Vector3 forward)
        {
            // Grid extends: half-size in X and Z
            float halfX = (gridSizeX * cellSize) * 0.5f;
            float halfZ = (gridSizeZ * cellSize) * 0.5f;

            Vector3 corner = origin - right * halfX + right * (cellSize * 0.5f) + forward * (cellSize * 0.5f);
            if (centerOnEnemy) return corner - forward * halfZ;
            return corner + forward * forwardOffset;
        }

        private void WriteCell(VectorSensor sensor, Vector3 cellCenter, Vector3 halfExtents)
        {
            int hitCount = Physics.OverlapBoxNonAlloc(cellCenter, halfExtents, _results, Quaternion.identity, detectionMask);

            // Build one-hot for this cell and write to sensor
            float[] oneHot = BuildOneHot(hitCount);
            for (int t = 0; t < encodedTags.Length; t++)
                sensor.AddObservation(oneHot[t]);
        }

        private float[] BuildOneHot(int hitCount)
        {
            float[] oneHot = new float[encodedTags.Length];
            for (int h = 0; h < hitCount && h < _results.Length; h++)
            {
                for (int t = 0; t < encodedTags.Length; t++)
                {
                    if (_results[h].CompareTag(encodedTags[t]))
                    {
                        oneHot[t] = 1f;
                        break;
                    }
                }
            }
            return oneHot;
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying && !enabled) return;

            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            float halfX = (gridSizeX * cellSize) * 0.5f;
            float halfZ = (gridSizeZ * cellSize) * 0.5f;

            Vector3 gridOrigin;
            if (centerOnEnemy)
                gridOrigin = origin - right * halfX - forward * halfZ;
            else
                gridOrigin = origin - right * halfX + forward * forwardOffset;

            Gizmos.color = new Color(0, 1, 1, 0.3f);
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    Vector3 cellCenter = gridOrigin + right * (x * cellSize + cellSize * 0.5f) + forward * (z * cellSize + cellSize * 0.5f);
                    Gizmos.DrawWireCube(cellCenter + Vector3.up * detectionHeight * 0.5f,
                        new Vector3(cellSize, detectionHeight, cellSize));
                }
            }
        }
    }
}
