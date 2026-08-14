using UnityEngine;
using UnityEngine.AI;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>
    /// Glue that wires an enemy's gameplay events into the framework:
    ///  - death        -> EnemyBrain.ReportDeath() + notify the MinigameManager
    ///  - attack hit   -> RewardCombatPerformance.RegisterHit() (and optional self-heal slot)
    ///  - item used    -> tactical movement (cover / flank / retreat) for the shooter branch
    /// </summary>
    [RequireComponent(typeof(EnemyBrain))]
    public class EnemyWiring : MonoBehaviour
    {
        [Header("Combat")]
        [Tooltip("Combat slot that acts as a self-heal (RPG). -1 disables.")]
        public int healSlotIndex = 3;

        [Tooltip("Health restored when the heal slot is used.")]
        public float healAmount = 25f;

        [Header("Tactical movement (shooter)")]
        [Tooltip("Search radius when looking for a cover object.")]
        public float coverSearchRadius = 30f;

        [Tooltip("Angle (degrees) to offset when flanking around the threat.")]
        public float flankAngle = 90f;

        [Tooltip("Distance to move when executing a tactical reposition.")]
        public float tacticalMoveDistance = 8f;

        private EnemyBrain _brain;
        private SimpleStatusProvider _status;
        private RewardCombatPerformance _combatReward;
        private ActionCombat _combat;
        private ActionItemUsage _items;
        private NavMeshAgent _agent;
        private SimpleTargetProvider _target;

        private void Awake()
        {
            _brain = GetComponent<EnemyBrain>();
            _status = GetComponent<SimpleStatusProvider>();
            _combatReward = GetComponent<RewardCombatPerformance>();
            _combat = GetComponent<ActionCombat>();
            _items = GetComponent<ActionItemUsage>();
            _agent = GetComponent<NavMeshAgent>();
            _target = GetComponent<SimpleTargetProvider>();
        }

        private void OnEnable()
        {
            if (_status != null) _status.OnDeath += HandleDeath;
            if (_combat != null) _combat.OnAttackExecuted.AddListener(HandleAttackExecuted);
            if (_items != null) _items.OnItemUsed.AddListener(HandleItemUsed);
        }

        private void OnDisable()
        {
            if (_status != null) _status.OnDeath -= HandleDeath;
            if (_combat != null) _combat.OnAttackExecuted.RemoveListener(HandleAttackExecuted);
            if (_items != null) _items.OnItemUsed.RemoveListener(HandleItemUsed);
        }

        private void Start()
        {
            if (_combat != null && _combat.attackTarget == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _combat.attackTarget = player.transform;
            }
        }

        private void HandleDeath()
        {
            if (_brain != null) _brain.ReportDeath();
            if (MinigameManager.Instance != null)
                MinigameManager.Instance.RegisterEnemyDeath(_brain);
        }

        private void HandleAttackExecuted(int slot, float damage)
        {
            if (damage > 0.001f && _combatReward != null)
                _combatReward.RegisterHit(damage);

            if (slot == healSlotIndex && healAmount > 0f && _status != null)
                _status.Heal(healAmount);
        }

        private void HandleItemUsed(int slot, Transform target)
        {
            // Shooter tactical branch: 0 = cover, 1 = flank, 2 = retreat.
            if (_agent == null || !_agent.isOnNavMesh) return;
            Transform threat = _target != null && _target.HasValidTarget ? _target.Target : null;

            switch (slot)
            {
                case 0: MoveToCover(threat); break;
                case 1: Flank(threat); break;
                case 2: Retreat(threat); break;
            }
        }

        private void MoveToCover(Transform threat)
        {
            var covers = GameObject.FindGameObjectsWithTag("Cover");
            Vector3 threatPos = threat != null ? threat.position : transform.position + transform.forward * 10f;
            Transform best = null;
            float bestScore = float.MaxValue;

            foreach (var c in covers)
            {
                if (c == null || c.transform == transform) continue;
                float distToSelf = Vector3.Distance(transform.position, c.transform.position);
                if (distToSelf > coverSearchRadius) continue;

                Vector3 coverPos = c.transform.position + (c.transform.position - threatPos).normalized * 1.5f;
                float score = distToSelf - Vector3.Distance(coverPos, threatPos) * 0.25f;
                if (score < bestScore) { bestScore = score; best = c.transform; }
            }

            if (best != null) MoveTo(best.position);
        }

        private void Flank(Transform threat)
        {
            Vector3 baseDir = threat != null
                ? (transform.position - threat.position).normalized
                : -transform.forward;

            Vector3 side = Vector3.Cross(Vector3.up, baseDir).normalized;
            Vector3 dest = transform.position + Quaternion.AngleAxis(flankAngle, Vector3.up) * side * tacticalMoveDistance;
            MoveTo(dest);
        }

        private void Retreat(Transform threat)
        {
            Vector3 away = threat != null
                ? (transform.position - threat.position).normalized
                : -transform.forward;

            MoveTo(transform.position + away * tacticalMoveDistance);
        }

        private bool MoveTo(Vector3 worldPos)
        {
            if (_agent == null || !_agent.isOnNavMesh) return false;
            if (NavMesh.SamplePosition(worldPos, out NavMeshHit hit, tacticalMoveDistance * 1.5f, NavMesh.AllAreas))
            {
                _agent.isStopped = false;
                _agent.SetDestination(hit.position);
                return true;
            }
            return false;
        }
    }
}

