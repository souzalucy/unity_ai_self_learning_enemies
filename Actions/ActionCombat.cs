using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Combat actions via discrete branching.
    /// 1 discrete branch: 0=none, 1=attack1, 2=attack2, ..., N-1=special.
    /// Calls UnityEvents or interface methods on assigned targets.
    /// </summary>
    public class ActionCombat : ActionEffect
    {
        [Tooltip("Number of combat action slots (excluding 'none' at index 0).")]
        [Range(1, 10)]
        public int actionSlotCount = 3;

        [Tooltip("Cooldowns per action slot (seconds).")]
        public float[] cooldowns = new float[] { 0.5f, 1.5f, 5f };

        [Tooltip("If set, attacks target this transform's GameObject.")]
        public Transform attackTarget;

        [Tooltip("Damage values per action slot.")]
        public float[] damageValues = new float[] { 10f, 25f, 50f };

        private float[] _cooldownTimers;
        private ICombatTarget _combatTarget;

        public override int DiscreteBranchCount => 1;
        public override int[] DiscreteBranchSizes => new int[] { actionSlotCount + 1 }; // +1 for "none"
        public override int ContinuousActionCount => 0;

        private void Awake()
        {
            _cooldownTimers = new float[actionSlotCount];
            if (attackTarget != null)
                _combatTarget = attackTarget.GetComponent<ICombatTarget>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _cooldownTimers.Length; i++)
                if (_cooldownTimers[i] > 0f) _cooldownTimers[i] -= dt;
        }

        public override void ApplyActions(float[] discreteActions, float[] continuousActions)
        {
            if (discreteActions.Length == 0) return;

            int chosen = Mathf.RoundToInt(discreteActions[0]);
            if (chosen <= 0 || chosen > actionSlotCount) return; // 0 = no action

            int slot = chosen - 1;
            if (slot >= _cooldownTimers.Length) return;
            if (_cooldownTimers[slot] > 0f) return; // on cooldown

            // Start cooldown
            _cooldownTimers[slot] = cooldowns.Length > slot ? cooldowns[slot] : 1f;

            // Apply damage
            float dmg = damageValues.Length > slot ? damageValues[slot] : 10f;

            if (_combatTarget != null)
            {
                _combatTarget.TakeDamage(dmg);
            }
            else if (attackTarget != null)
            {
                // Fallback: try SendMessage or check for IDamageable
                var damageable = attackTarget.GetComponent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(dmg);
            }

            // Fire UnityEvent for VFX / animation
            OnAttackExecuted?.Invoke(slot, dmg);
        }

        [System.Serializable]
        public class AttackEvent : UnityEngine.Events.UnityEvent<int, float> { }
        public AttackEvent OnAttackExecuted;
    }

    /// <summary>
    /// Interface for objects that can receive combat damage.
    /// Implement on your player / destructible objects.
    /// </summary>
    public interface ICombatTarget
    {
        void TakeDamage(float amount);
    }

    /// <summary>
    /// Alternative simpler interface for damage.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }
}
