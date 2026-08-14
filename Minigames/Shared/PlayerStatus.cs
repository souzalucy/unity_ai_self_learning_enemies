using UnityEngine;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>
    /// Player-specific status wrapper. Routes player death to the MinigameManager and
    /// exposes health/revive accessors for the manager and HUD.
    /// </summary>
    [RequireComponent(typeof(SimpleStatusProvider))]
    public class PlayerStatus : MonoBehaviour
    {
        [SerializeField] private SimpleStatusProvider _status;

        /// <summary>Fired when the player dies (before the manager is notified).</summary>
        public event System.Action<PlayerStatus> OnPlayerDied;

        public float Health => _status != null ? _status.Health : 0f;
        public float MaxHealth => _status != null ? _status.MaxHealth : 0f;
        public bool IsAlive => _status != null && _status.IsAlive;

        private void Awake()
        {
            gameObject.tag = "Player";
            if (_status == null) _status = GetComponent<SimpleStatusProvider>();
            if (_status == null) _status = gameObject.AddComponent<SimpleStatusProvider>();
        }

        private void OnEnable()
        {
            if (_status != null) _status.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            if (_status != null) _status.OnDeath -= HandleDeath;
        }

        public void Heal(float amount)
        {
            if (_status != null) _status.Heal(amount);
        }

        public void Revive(float healthPercent = 1f)
        {
            if (_status != null) _status.Revive(healthPercent);
        }

        private void HandleDeath()
        {
            OnPlayerDied?.Invoke(this);
            if (MinigameManager.Instance != null)
                MinigameManager.Instance.RegisterPlayerDeath();
        }
    }
}
