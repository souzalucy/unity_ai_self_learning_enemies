using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Interface for providing status values (health, mana, shield) to observation and reward components.
    /// Implement this on your character/entity component to decouple the AI from your game's data model.
    ///
    /// Instead of setting ObsSelfStatus.health directly, implement IStatusProvider on your health component
    /// and ObsSelfStatus will read from it automatically.
    /// </summary>
    public interface IStatusProvider
    {
        float Health { get; }
        float MaxHealth { get; }
        float SecondaryResource { get; }
        float MaxSecondaryResource { get; }
        float Shield { get; }
        float MaxShield { get; }
        bool IsAlive { get; }

        /// <summary>
        /// Optional: event fired when health changes significantly.
        /// Subscribe in reward sources to react to damage/heal events.
        /// </summary>
        event System.Action<float, float> OnHealthChanged; // (oldHealth, newHealth)
        event System.Action OnDeath;
    }

    /// <summary>
    /// Simple default implementation of IStatusProvider you can use or replace.
    /// </summary>
    public class SimpleStatusProvider : MonoBehaviour, IStatusProvider
    {
        [SerializeField] private float _health = 100f;
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _secondary = 100f;
        [SerializeField] private float _maxSecondary = 100f;
        [SerializeField] private float _shield;
        [SerializeField] private float _maxShield = 100f;
        [SerializeField] private bool _isAlive = true;

        public float Health => _health;
        public float MaxHealth => _maxHealth;
        public float SecondaryResource => _secondary;
        public float MaxSecondaryResource => _maxSecondary;
        public float Shield => _shield;
        public float MaxShield => _maxShield;
        public bool IsAlive => _isAlive;

        public event System.Action<float, float> OnHealthChanged;
        public event System.Action OnDeath;

        public void TakeDamage(float amount)
        {
            if (!_isAlive) return;
            float old = _health;

            // Apply to shield first
            if (_shield > 0)
            {
                float shieldDmg = Mathf.Min(amount, _shield);
                _shield -= shieldDmg;
                amount -= shieldDmg;
            }

            _health = Mathf.Max(0, _health - amount);
            OnHealthChanged?.Invoke(old, _health);

            if (_health <= 0 && _isAlive)
            {
                _isAlive = false;
                OnDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (!_isAlive) return;
            float old = _health;
            _health = Mathf.Min(_maxHealth, _health + amount);
            OnHealthChanged?.Invoke(old, _health);
        }

        public void Revive(float healthPercent = 1f)
        {
            _isAlive = true;
            _health = _maxHealth * healthPercent;
            _secondary = _maxSecondary;
            _shield = _maxShield;
        }
    }
}
