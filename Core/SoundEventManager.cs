using System.Collections.Generic;
using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Represents a sound event in the world that enemies can perceive.
    /// </summary>
    public struct SoundEvent
    {
        public Vector3 position;
        public float intensity;      // 0..1 normalized loudness
        public float time;           // Time.time when emitted
        public SoundType type;       // Categorization for one-hot encoding

        public SoundEvent(Vector3 pos, float intensity, SoundType type)
        {
            position = pos;
            this.intensity = Mathf.Clamp01(intensity);
            time = Time.time;
            this.type = type;
        }

        public float Age => Time.time - time;
    }

    /// <summary>
    /// Categories of sounds for one-hot encoding in observations.
    /// Order matches the one-hot encoding in ObsSoundPerception.
    /// </summary>
    public enum SoundType
    {
        Footstep = 0,
        Gunshot = 1,
        Explosion = 2,
        Ability = 3,
        Voice = 4,
        Vehicle = 5,
        Impact = 6,
        Other = 7
    }

    /// <summary>
    /// Global manager for sound events. Any object can emit a sound via Emit(), and
    /// ObsSoundPerception queries GetRecentSounds() to perceive the environment.
    /// </summary>
    public class SoundEventManager : MonoBehaviour
    {
        private static SoundEventManager _instance;
        public static SoundEventManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SoundEventManager");
                    _instance = go.AddComponent<SoundEventManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Tooltip("Maximum number of sound events to keep in history.")]
        public int maxEvents = 64;

        [Tooltip("Events older than this (seconds) are pruned.")]
        public float maxAge = 5f;

        private readonly List<SoundEvent> _events = new List<SoundEvent>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        /// <summary>
        /// Emit a sound event into the world. Call from any script when a sound-producing action occurs.
        /// </summary>
        public static void Emit(Vector3 position, float intensity, SoundType type)
        {
            Instance._events.Add(new SoundEvent(position, intensity, type));
        }

        /// <summary>
        /// Get all recent sound events within the specified range, sorted by recency.
        /// </summary>
        public List<SoundEvent> GetRecentSounds(Vector3 listenerPosition, float hearingRange, int maxCount)
        {
            // Prune old events
            float now = Time.time;
            _events.RemoveAll(e => e.Age > maxAge);
            if (_events.Count > maxEvents)
                _events.RemoveRange(0, _events.Count - maxEvents);

            var result = new List<SoundEvent>();
            float rangeSq = hearingRange * hearingRange;

            // Collect in-range events, newest first
            for (int i = _events.Count - 1; i >= 0 && result.Count < maxCount; i--)
            {
                float distSq = (_events[i].position - listenerPosition).sqrMagnitude;
                if (distSq <= rangeSq)
                    result.Add(_events[i]);
            }

            return result;
        }

        /// <summary>
        /// Clear all sound events (e.g., on scene change).
        /// </summary>
        public void Clear() => _events.Clear();
    }
}
