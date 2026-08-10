using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Base class for procedural training arena builders.
    /// Creates a training environment with spawn points, boundaries, and configuration.
    /// Attach a genre-specific subclass to an empty GameObject and press Build in the inspector.
    /// </summary>
    public abstract class TrainingArenaBuilder : MonoBehaviour
    {
        [Header("Arena")]
        public Vector2 arenaSize = new Vector2(50f, 50f);
        public float wallHeight = 3f;
        public Material floorMaterial;
        public Material wallMaterial;
        public bool clearOnBuild = true;

        [Header("Spawns")]
        public int enemyCount = 4;
        public float minSpawnDistance = 5f;
        public Vector3 playerSpawnOffset = new Vector3(0f, 1f, -15f);
        public GameObject playerPrefab;
        public GameObject enemyPrefab;

        protected GameObject _arenaRoot;
        protected Transform[] _spawnPoints;
        protected GameObject _playerInstance;

        protected abstract void BuildGenreFeatures();

        [ContextMenu("Build Arena")]
        public void BuildArena()
        {
            if (clearOnBuild)
                foreach (Transform child in transform) DestroyImmediate(child.gameObject);

            _arenaRoot = new GameObject("Arena");
            _arenaRoot.transform.SetParent(transform);

            BuildFloor();
            BuildWalls();
            BuildGenreFeatures();
            CreateSpawnPoints();
            SpawnEntities();
            Debug.Log($"[{GetType().Name}] Arena built: {arenaSize.x}x{arenaSize.y}, {enemyCount} enemies.");
        }


        protected virtual void BuildFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor"; floor.transform.SetParent(_arenaRoot.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(arenaSize.x / 10f, 1f, arenaSize.y / 10f);
            if (floorMaterial != null) floor.GetComponent<Renderer>().material = floorMaterial;
        }

        protected virtual void BuildWalls()
        {
            float hw = arenaSize.x * 0.5f, hz = arenaSize.y * 0.5f, hh = wallHeight * 0.5f;
            CreateWall("Wall_N", new Vector3(0, hh, hz), new Vector3(arenaSize.x, wallHeight, 0.2f));
            CreateWall("Wall_S", new Vector3(0, hh, -hz), new Vector3(arenaSize.x, wallHeight, 0.2f));
            CreateWall("Wall_E", new Vector3(hw, hh, 0), new Vector3(0.2f, wallHeight, arenaSize.y));
            CreateWall("Wall_W", new Vector3(-hw, hh, 0), new Vector3(0.2f, wallHeight, arenaSize.y));
        }

        protected GameObject CreateWall(string n, Vector3 pos, Vector3 scl)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = n; w.transform.SetParent(_arenaRoot.transform);
            w.transform.localPosition = pos; w.transform.localScale = scl;
            if (wallMaterial != null) w.GetComponent<Renderer>().material = wallMaterial;
            return w;
        }

        protected virtual void CreateSpawnPoints()
        {
            _spawnPoints = new Transform[enemyCount];
            var root = new GameObject("SpawnPoints"); root.transform.SetParent(_arenaRoot.transform);
            float hw = arenaSize.x * 0.4f, hz = arenaSize.y * 0.4f;
            for (int i = 0; i < enemyCount; i++)
            {
                var sp = new GameObject($"Spawn_{i}"); sp.transform.SetParent(root.transform);
                Vector3 pos; int att = 0;
                do { pos = new Vector3(Random.Range(-hw, hw), 1f, Random.Range(-hz, hz)); att++; }
                while (att < 50 && !IsSpawnValid(pos));
                sp.transform.localPosition = pos; _spawnPoints[i] = sp.transform;
            }
        }

        protected bool IsSpawnValid(Vector3 pos)
        {
            if (_spawnPoints == null) return true;
            foreach (var sp in _spawnPoints)
                if (sp != null && Vector3.Distance(pos, sp.localPosition) < minSpawnDistance) return false;
            return Vector3.Distance(pos, playerSpawnOffset) >= minSpawnDistance * 1.5f;
        }

        protected virtual void SpawnEntities()
        {
            if (playerPrefab != null) { _playerInstance = Instantiate(playerPrefab, playerSpawnOffset, Quaternion.identity); _playerInstance.name = "Player"; }
            if (enemyPrefab != null && _spawnPoints != null)
                foreach (var sp in _spawnPoints) { var e = Instantiate(enemyPrefab, sp.position, Quaternion.identity); e.name = $"Enemy_{sp.name}"; }
        }
    }
}
