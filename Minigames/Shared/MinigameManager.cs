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
        [SerializeField] private RoundStateMachine _round = new RoundStateMachine();

        private readonly List<EnemyBrain> _enemies = new List<EnemyBrain>();
        private bool _resetting;

        private readonly RacingRoundController _racing = new RacingRoundController();

        public int Score => _round.Score;
        public int CurrentWave => _round.CurrentWave;
        public float TimeRemaining => _round.TimeRemaining;
        public bool IsGameOver => _round.IsGameOver;
        public bool PlayerWon => _round.PlayerWon;
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
            if (_resetting || settings == null) return;
            if (_round.Tick(Time.deltaTime, settings.timeLimitSeconds > 0f)) HandleTimeUp();
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
            Player = EnemySpawner.SpawnPlayer(settings, playerSpawnOffset);
        }

        private void SpawnEnemies()
        {
            _enemies.Clear();
            _enemies.AddRange(EnemySpawner.SpawnEnemies(settings, arenaBuilder, Player != null ? Player.transform : null));
        }

        private void SetupRacing()
        {
            if (settings.genre != EnemyGenre.Racing) return;
            var track = arenaBuilder as RacingTrackBuilder;
            if (track == null || track.Waypoints == null || track.Waypoints.Count == 0) return;

            _racing.ConfigureCheckpoints(track);
            _racing.AssignWaypoints(Enemies, track.Waypoints);

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
            _round.BeginRound(settings.timeLimitSeconds);
            ResetEntities();
        }

        private void ResetEntities()
        {
            ResetPlayer();
            ResetEnemies();
            _racing.Reset();
            OnRoundReset?.Invoke();
        }

        private void ResetPlayer()
        {
            if (Player == null) return;
            Player.Revive(1f);
            Player.transform.position = GetPlayerSpawnPosition();
            var rb = Player.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
            var nav = Player.GetComponent<NavMeshAgent>();
            if (nav != null && nav.isOnNavMesh) nav.ResetPath();
        }

        private void ResetEnemies()
        {
            Vector3[] spawns = EnemySpawner.ComputeEnemySpawns(settings, arenaBuilder, _enemies.Count);
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
        }

        /// <summary>Called by EnemyWiring when an enemy dies.</summary>
        public void RegisterEnemyDeath(EnemyBrain brain)
        {
            if (_round.IsGameOver || _resetting) return;
            _round.AddScore();
            OnEnemyKilled?.Invoke(brain);

            if (AllEnemiesDead()) HandlePlayerSurvived();
        }

        private bool AllEnemiesDead()
        {
            if (_enemies.Count == 0) return false;
            foreach (var e in _enemies)
            {
                if (e == null) continue;
                var status = e.GetComponent<SimpleStatusProvider>();
                if (status != null && status.IsAlive) return false;
            }
            return true;
        }

        /// <summary>Called by PlayerStatus when the player dies.</summary>
        public void RegisterPlayerDeath()
        {
            if (_round.IsGameOver || _resetting) return;
            OnPlayerDied?.Invoke();
            EndGame(playerWon: false);
        }

        /// <summary>Called by TrackCheckpoint when a vehicle crosses a waypoint.</summary>
        public void OnCheckpointPassed(GameObject vehicle, int waypointIndex)
        {
            if (!IsRacingRoundActive(vehicle)) return;

            bool isPlayer = Player != null && vehicle == Player.gameObject;
            if (_racing.OnCheckpointPassed(vehicle, waypointIndex, settings.lapsToWin, isPlayer, out bool playerWon))
                EndGame(playerWon);
        }

        private bool IsRacingRoundActive(GameObject vehicle)
        {
            if (settings == null || settings.genre != EnemyGenre.Racing) return false;
            return !_round.IsGameOver && !_resetting && vehicle != null;
        }

        /// <summary>Lap count for a specific vehicle (HUD / diagnostics).</summary>
        public int GetLaps(GameObject vehicle) => _racing.GetLaps(vehicle);

        private int PlayerLaps() => _racing.PlayerLaps(Player);

        private int BestEnemyLaps() => _racing.BestEnemyLaps(Enemies);

        private void HandleTimeUp()
        {
            if (_round.IsGameOver || _resetting) return;
            if (settings.genre == EnemyGenre.Racing)
                EndGame(playerWon: PlayerLaps() >= BestEnemyLaps());
            else
                EndGame(playerWon: false);
        }

        private void HandlePlayerSurvived()
        {
            if (settings.genre == EnemyGenre.RPG && _round.CanAdvanceWave(settings.wavesToSurvive))
            {
                _round.AdvanceWave();
                StartCoroutine(BeginNextWaveAfterDelay());
            }
            else
            {
                EndGame(playerWon: true);
            }
        }

        private void EndGame(bool playerWon)
        {
            _round.End(playerWon);

            if (!playerWon)
            {
                RewardSurvivingEnemies();
                OnDefeat?.Invoke();
            }
            else
            {
                OnVictory?.Invoke();
            }

            StartCoroutine(RestartAfterDelay());
        }

        private void RewardSurvivingEnemies()
        {
            foreach (var brain in _enemies)
            {
                if (brain == null) continue;
                var status = brain.GetComponent<SimpleStatusProvider>();
                if (status != null && status.IsAlive)
                    brain.ReportObjectiveComplete();
            }
        }

        private IEnumerator RestartAfterDelay()
        {
            _resetting = true;
            yield return new WaitForSeconds(settings.roundResetDelay);
            _resetting = false;
            _round.ResetScore();
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


