using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Component discovery, hashing, and mid-episode safety refresh.
    /// </summary>
    public partial class EnemyBrain
    {
        private int _componentHash;
        private bool _isRefreshing;

        public void CacheComponents()
        {
            _observationSources = GetComponents<ObservationSource>();
            _actionEffects = GetComponents<ActionEffect>();
            _rewardSources = GetComponents<RewardSource>();
        }

        /// <summary>
        /// Safe to call mid-episode. Detects if components changed and rebuilds action mapping if needed.
        /// Returns true if a rebuild occurred.
        /// </summary>
        public bool SafeRefreshComponents()
        {
            if (_isRefreshing) return false;
            _isRefreshing = true;
            int newHash = ComputeComponentHash();
            if (newHash == _componentHash)
            {
                _isRefreshing = false;
                return false;
            }

            if (debugMode)
                Debug.Log($"[EnemyBrain] Component change detected at step {_episodeStep}. Rebuilding action space.");
            CacheComponents();
            ConfigureActionSpace();
            _componentHash = newHash;
            _isRefreshing = false;
            return true;
        }

        private int ComputeComponentHash()
        {
            unchecked
            {
                int hash = 17;
                hash = HashComponents(hash, GetComponents<ObservationSource>());
                hash = HashComponents(hash, GetComponents<ActionEffect>());
                hash = HashComponents(hash, GetComponents<RewardSource>());
                return hash;
            }
        }

        private static int HashComponents<T>(int hash, T[] components) where T : class
        {
            foreach (var c in components)
                hash = hash * 31 + (c?.GetHashCode() ?? 0);
            return hash;
        }
    }
}