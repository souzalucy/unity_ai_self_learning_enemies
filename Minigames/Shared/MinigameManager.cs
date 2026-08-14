using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>
    /// Central game-loop coordinator for all minigames. Builds the arena (optional),
    /// spawns the player + enemies, tracks win/lose state, and calls the framework's
    /// public reward API (ReportObjectiveComplete / ReportDeath) at the right moments.
    /// </summary>
    public class MinigameManager : MonoBehaviour
    {
        public static MinigameManager Instance { get; private set; }

        [Header("Setup")]
        public MinigameSettings settings;

        [Tooltip("Optional arena builder. If set and buildArenaOnStart, geometry is built at Start.")]
        public TrainingArenaBuilder arenaBuilder;

        [Tooltip("Whether to (re)build the arena geometry at Start. Keep true for procedural minigames.")]
        public bool buildArenaOnStart = true;

        [Tooltip("Player spawn position (RPG/Shooter). Racing repositions to the start line.")]
        public Vector3 playerSpawnOffset = new Vector3(0f, 1f, -15f);

        [Header("Runtime (read-only)")]
        [SerializeField] private int _score;
        [SerializeField] private int _currentWave = 1;
        [SerializeField] private float _timeRemaining;
        [SerializeField] private bool _gameOver;
        [SerializeField] private bool _playerWon;

        private readonly List<EnemyBrain> _enemies = new List<EnemyBrain>();
        private bool _resetting;

        private readonly Dictionary<int, int> _lastCheckpointByInstance = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _lapByInstance = new Dictionary<int, int>();
        private int _waypointCount;

        public int Score => _score;
        public int CurrentWave => _currentWave;
        public float TimeRemaining => _timeRemaining;
        public bool IsGameOver => _gameOver;
        public bool PlayerWon => _playerWon;
        public PlayerStatus Player { get; private set; }
        public IReadOnlyList<EnemyBrain> Enemies => _enemies;
        public int LapsToWin => settings != null ? settings.lapsToWin : 1;

        public event System.Action OnRoundReset;
        public event System.Action OnPlayerDied;
        public event System.Action<EnemyBrain> OnEnemyKilled;
        public event System.Action OnVictory;
        public event System.Action OnDefeat;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (settings == null)
            {
                Debug.LogError("[MinigameManager] No MinigameSettings assigned. Disabling.");
                enabled = false;
                return;
            }

            Time.timeScale = settings.timeScale;
            BuildArenaGeometry();
            SpawnPlayer();
            SpawnEnemies();
            SetupRacing();
            BeginRound();
        }

        private void Update()
        {
            if (_gameOver || _resetting || settings == null) return;
            if (settings.timeLimitSeconds <= 0f) return;

            _timeRemaining -= Time.deltaTime;
            if (_timeRemaining <= 0f) HandleTimeUp();
        }

        // ------------------------------------------------------------------
        // Setup
        // ------------------------------------------------------------------

        private void BuildArenaGeometry()
        {
            if (arenaBuilder == null) return;
            // The manager owns entity spawning; prevent the builder from double-spawning.
            arenaBuilder.playerPrefab = null;
            arenaBuilder.enemyPrefab = null;
            arenaBuilder.enemyCount = settings.enemyCount;
            if (buildArenaOnStart) arenaBuilder.BuildArena();
        }

        private void SpawnPlayer()
        {
            GameObject go;
            if (settings.playerPrefab != null)
            {
                go = Instantiate(settings.playerPrefab, playerSpawnOffset, Quaternion.identity);
            }
            else
            {
                go = GameObject.CreatePrimitive(settings.genre == EnemyGenre.Racing ? PrimitiveType.Cube : PrimitiveType.Capsule);
                go.transform.position = playerSpawnOffset;
            }
            go.name = "Player";
            MinigameComposer.ConfigurePlayer(go, settings);
            Player = go.GetComponent<PlayerStatus>();
        }

        private void SpawnEnemies()
        {
            _enemies.Clear();
            Vector3[] spawns = ComputeEnemySpawns(settings.enemyCount);

            for (int i = 0; i < settings.enemyCount; i++)
            {
                Vector3 pos = i < spawns.Length ? spawns[i] : spawns[0] + Vector3.right * i * 2f;

                GameObject go;
                if (settings.enemyPrefab != null)
                {
                    go = Instantiate(settings.enemyPrefab, pos, Quaternion.identity);
                }
                else
                {
                    go = GameObject.CreatePrimitive(settings.genre == EnemyGenre.Racing ? PrimitiveType.Cube : PrimitiveType.Capsule);
                    go.transform.position = pos;
                }
                go.name = $"Enemy_{i}";

                var brain = MinigameComposer.ConfigureEnemy(go, settings, Player != null ? Player.transform : null);
                if (brain != null) _enemies.Add(brain);
            }
        }

        private Vector3[] ComputeEnemySpawns(int count)
        {
            if (arenaBuilder is RacingTrackBuilder track && track.Waypoints != null && track.Waypoints.Count > 0)
            {
                var result = new Vector3[count];
                for (int i = 0; i < count; i++)
                {
                    int idx = (i * track.Waypoints.Count / Mathf.Max(1, count)) % track.Waypoints.Count;
                    result[i] = track.Waypoints[idx].position + Vector3.right * (i % 2 == 0 ? 2f : -2f);
                }
                return result;
            }

            Vector2 size = arenaBuilder != null ? arenaBuilder.arenaSize : new Vector2(50f, 50f);
            float radius = Mathf.Min(size.x, size.y) * 0.35f;
            var spawns = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float angle = count > 1 ? (360f / count) * i : 0f;
                spawns[i] = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, 1f, Mathf.Sin(angle * Mathf.Deg2Rad) * radius);
            }
            return spawns;
        }

        private void SetupRacing()
        {
            if (settings.genre != EnemyGenre.Racing) return;
            var track = arenaBuilder as RacingTrackBuilder;
            if (track == null || track.Waypoints == null || track.Waypoints.Count == 0) return;

            _waypointCount = track.Waypoints.Count;
            var wps = track.Waypoints;

            // Add checkpoint triggers to each waypoint.
            for (int i = 0; i < wps.Count; i++)
            {
                if (wps[i] == null) continue;
                var cp = wps[i].GetComponent<TrackCheckpoint>();
                if (cp == null) cp = wps[i].gameObject.AddComponent<TrackCheckpoint>();
                cp.waypointIndex = i;

                var col = wps[i].GetComponent<Collider>();
                if (col == null)
                {
                    var box = wps[i].gameObject.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.size = new Vector3(8f, 2f, 2f);
                }
                else
                {
                    col.isTrigger = true;
                }
            }

            foreach (var brain in _enemies)
            {
                if (brain == null) continue;
                var obs = brain.GetComponent<ObsWaypointProgress>();
                if (obs != null) obs.waypoints = wps.ToArray();
                var rwd = brain.GetComponent<RewardWaypointProgress>();
                if (rwd != null) rwd.waypoints = wps.ToArray();
            }

            if (Player != null)
                Player.transform.position = GetPlayerSpawnPosition();
        }

        private Vector3 GetPlayerSpawnPosition()
        {
            if (settings != null && settings.genre == EnemyGenre.Racing
                && arenaBuilder is RacingTrackBuilder track
                && track.Waypoints != null && track.Waypoints.Count > 0)
                return track.Waypoints[0].position + Vector3.up * 1f;
            return playerSpawnOffset;
        }

        // ------------------------------------------------------------------
        // Game loop
        // ------------------------------------------------------------------

        private void BeginRound()
        {
            _timeRemaining = settings.timeLimitSeconds;
            _gameOver = false;
            _playerWon = false;
            ResetEntities();
        }

        private void ResetEntities()
        {
            if (Player != null)
            {
                Player.Revive(1f);
                Player.transform.position = GetPlayerSpawnPosition();
                var rb = Player.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;
                var nav = Player.GetComponent<NavMeshAgent>();
                if (nav != null && nav.isOnNavMesh) nav.ResetPath();
            }

            Vector3[] spawns = ComputeEnemySpawns(_enemies.Count);
            for (int i = 0; i < _enemies.Count; i++)
            {
                var brain = _enemies[i];
                if (brain == null) continue;

                var status = brain.GetComponent<SimpleStatusProvider>();
                if (status != null) status.Revive(1f);

                if (i < spawns.Length) brain.transform.position = spawns[i];

                var rb = brain.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;
            }

            _lastCheckpointByInstance.Clear();
            _lapByInstance.Clear();
            OnRoundReset?.Invoke();
        }

        /// <summary>Called by EnemyWiring when an enemy dies.</summary>
        public void RegisterEnemyDeath(EnemyBrain brain)
        {
            if (_gameOver || _resetting) return;
            _score++;
            OnEnemyKilled?.Invoke(brain);

            bool allDead = _enemies.Count > 0;
            foreach (var e in _enemies)
            {
                if (e == null) continue;
                var status = e.GetComponent<SimpleStatusProvider>();
                if (status != null && status.IsAlive) { allDead = false; break; }
            }

            if (allDead) HandlePlayerSurvived();
        }

        /// <summary>Called by PlayerStatus when the player dies.</summary>
        public void RegisterPlayerDeath()
        {
            if (_gameOver || _resetting) return;
            OnPlayerDied?.Invoke();
            EndGame(playerWon: false);
        }

        /// <summary>Called by TrackCheckpoint when a vehicle crosses a waypoint.</summary>
        public void OnCheckpointPassed(GameObject vehicle, int waypointIndex)
        {
            if (settings == null || settings.genre != EnemyGenre.Racing) return;
            if (_gameOver || _resetting || vehicle == null) return;

            int id = vehicle.GetInstanceID();
            if (!_lastCheckpointByInstance.TryGetValue(id, out int last)) last = -1;
            if (!_lapByInstance.TryGetValue(id, out int laps)) laps = 0;

            if (waypointIndex == 0 && last >= _waypointCount - 1)
                laps++;

            _lastCheckpointByInstance[id] = waypointIndex;
            _lapByInstance[id] = laps;

            if (laps >= settings.lapsToWin)
            {
                bool isPlayer = Player != null && vehicle == Player.gameObject;
                EndGame(playerWon: isPlayer);
            }
        }

        /// <summary>Lap count for a specific vehicle (HUD / diagnostics).</summary>
        public int GetLaps(GameObject vehicle)
        {
            if (vehicle == null) return 0;
            return _lapByInstance.TryGetValue(vehicle.GetInstanceID(), out int laps) ? laps : 0;
        }

        private int PlayerLaps() => GetLaps(Player != null ? Player.gameObject : null);

        private int BestEnemyLaps()
        {
            int best = 0;
            foreach (var e in _enemies)
                if (e != null) best = Mathf.Max(best, GetLaps(e.gameObject));
            return best;
        }

        private void HandleTimeUp()
        {
            if (_gameOver || _resetting) return;
            if (settings.genre == EnemyGenre.Racing)
                EndGame(playerWon: PlayerLaps() >= BestEnemyLaps());
            else
                EndGame(playerWon: false);
        }

        private void HandlePlayerSurvived()
        {
            if (settings.genre == EnemyGenre.RPG && _currentWave < settings.wavesToSurvive)
            {
                _currentWave++;
                StartCoroutine(BeginNextWaveAfterDelay());
            }
            else
            {
                EndGame(playerWon: true);
            }
        }

        private void EndGame(bool playerWon)
        {
            _gameOver = true;
            _playerWon = playerWon;

            if (!playerWon)
            {
                foreach (var brain in _enemies)
                {
                    if (brain == null) continue;
                    var status = brain.GetComponent<SimpleStatusProvider>();
                    if (status != null && status.IsAlive)
                        brain.ReportObjectiveComplete();
                }
                OnDefeat?.Invoke();
            }
            else
            {
                OnVictory?.Invoke();
            }

            StartCoroutine(RestartAfterDelay());
        }

        private IEnumerator RestartAfterDelay()
        {
            _resetting = true;
            yield return new WaitForSeconds(settings.roundResetDelay);
            _resetting = false;
            _score = 0;
            _currentWave = 1;
            BeginRound();
        }

        private IEnumerator BeginNextWaveAfterDelay()
        {
            _resetting = true;
            yield return new WaitForSeconds(settings.roundResetDelay);
            _resetting = false;
            ResetEntities();
        }
    }
}


