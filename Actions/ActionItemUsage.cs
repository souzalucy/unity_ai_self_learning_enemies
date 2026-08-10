using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Item/ability usage via discrete branching.
    /// 1 discrete branch: 0=none, 1=use_slot_0, ..., N=use_slot_N.
    /// When an item is used, fires a UnityEvent that the user wires up in the inspector.
    /// </summary>
    public class ActionItemUsage : ActionEffect
    {
        [Tooltip("Number of item/ability slots (excluding 'none' at index 0).")]
        [Range(1, 6)]
        public int slotCount = 2;

        [Tooltip("Cooldowns per slot (seconds).")]
        public float[] cooldowns = new float[] { 2f, 5f };

        [Tooltip("If true, items require a target.")]
        public bool requiresTarget = false;

        [Tooltip("Target for targeted items.")]
        public Transform itemTarget;

        private float[] _cooldownTimers;

        public override int DiscreteBranchCount => 1;
        public override int[] DiscreteBranchSizes => new int[] { slotCount + 1 };
        public override int ContinuousActionCount => 0;

        private void Awake()
        {
            _cooldownTimers = new float[slotCount];
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
            if (chosen <= 0 || chosen > slotCount) return;

            int slot = chosen - 1;
            if (slot >= _cooldownTimers.Length) return;
            if (_cooldownTimers[slot] > 0f) return;

            _cooldownTimers[slot] = cooldowns.Length > slot ? cooldowns[slot] : 1f;

            if (requiresTarget && itemTarget == null) return;

            OnItemUsed?.Invoke(slot, itemTarget);
        }

        [System.Serializable]
        public class ItemEvent : UnityEngine.Events.UnityEvent<int, Transform> { }
        public ItemEvent OnItemUsed;
    }
}
