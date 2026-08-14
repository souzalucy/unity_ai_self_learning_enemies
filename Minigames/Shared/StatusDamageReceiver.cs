using UnityEngine;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>
    /// Generic damage receiver that forwards incoming damage to a SimpleStatusProvider.
    /// Implements both ICombatTarget and IDamageable so ActionCombat, ActionAimAndShoot,
    /// and the player controller can all damage the same GameObject uniformly.
    /// </summary>
    [RequireComponent(typeof(SimpleStatusProvider))]
    public class StatusDamageReceiver : MonoBehaviour, ICombatTarget, IDamageable
    {
        private SimpleStatusProvider _status;

        private void Awake()
        {
            _status = GetComponent<SimpleStatusProvider>();
        }

        public void TakeDamage(float amount)
        {
            if (_status == null) _status = GetComponent<SimpleStatusProvider>();
            if (_status != null) _status.TakeDamage(amount);
            SoundEventManager.Emit(transform.position, 0.6f, SoundType.Impact);
        }
    }
}
