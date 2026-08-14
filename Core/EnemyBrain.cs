using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace SelfLearningEnemies
{
    [RequireComponent(typeof(BehaviorParameters))]
    [RequireComponent(typeof(DecisionRequester))]
    public partial class EnemyBrain : Agent
    {
        /// <summary>
        /// Default ML-Agents behavior name used by the training YAML configs. It must match
        /// the key under <c>behaviors:</c> in <c>Training/*.yaml</c> (currently "EnemyBrain").
        /// The composer and editor assign this to <see cref="BehaviorParameters.BehaviorName"/>
        /// so <c>mlagents-learn</c> can match the agent to its trainer config.
        /// </summary>
        public const string DefaultBehaviorName = "EnemyBrain";

        [Header("Configuration")]
        public GenreProfile genreProfile;
        public bool debugMode = false;

        [Header("Heuristic Key Bindings")]
        public KeyCode forwardKey = KeyCode.W;
        public KeyCode backKey = KeyCode.S;
        public KeyCode leftKey = KeyCode.A;
        public KeyCode rightKey = KeyCode.D;
        public KeyCode action1Key = KeyCode.Space;
        public KeyCode action2Key = KeyCode.E;
        public KeyCode action3Key = KeyCode.Q;

        private ObservationSource[] _observationSources;
        private ActionEffect[] _actionEffects;
        private RewardSource[] _rewardSources;
        private int[] _actionEffectDiscreteStart;
        private int[] _actionEffectContinuousStart;
        private float _episodeReward;
        private int _episodeStep;

        protected override void Awake()
        {
            base.Awake();
            CacheComponents();
            ConfigureActionSpace();
            _componentHash = ComputeComponentHash();
        }

        public override void Initialize()
        {
            base.Initialize();
            CacheComponents();
            ConfigureActionSpace();
        }

        public override void OnEpisodeBegin()
        {
            _episodeReward = 0f;
            _episodeStep = 0;
            foreach (var src in _observationSources) src?.OnEpisodeBegin();
            foreach (var eff in _actionEffects) eff?.OnEpisodeBegin();
            foreach (var rwd in _rewardSources) rwd?.OnEpisodeBegin();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            var sources = _observationSources;
            if (sources == null || sources.Length == 0) return;
            foreach (var src in sources)
                CollectFromSource(sensor, src);
        }

        private void CollectFromSource(VectorSensor sensor, ObservationSource src)
        {
            if (src == null) return;
            int before = sensor.ObservationSize();
            src.CollectObservations(sensor);
            int added = sensor.ObservationSize() - before;
            if (added != src.ObservationSize && src.ObservationSize > 0)
                Debug.LogError($"[EnemyBrain] {src.SourceName} size mismatch: declared {src.ObservationSize}, added {added}", this);
        }

        public void ReportObjectiveComplete()
        {
            float r = genreProfile != null ? genreProfile.objectiveCompleteReward : 1f;
            AddReward(r);
            _episodeReward += r;
            EndEpisode();
        }

        public void ReportDeath()
        {
            float p = genreProfile != null ? genreProfile.deathPenalty : -1f;
            AddReward(p);
            _episodeReward += p;
            EndEpisode();
        }

        public int GetTotalObservationSize()
        {
            int total = 0;
            foreach (var src in _observationSources)
                if (src != null) total += src.ObservationSize;
            return total;
        }

        public bool ValidateSetup()
        {
            bool valid = ValidateComponentPresence();
            int obs = GetTotalObservationSize();
            int actSrc = _actionEffects?.Length ?? 0;
            int rwdSrc = _rewardSources?.Length ?? 0;
            Debug.Log($"[EnemyBrain] Validation {(valid ? "PASSED" : "FAILED")}: {obs} obs, {actSrc} actions, {rwdSrc} rewards.");
            return valid;
        }

        private bool ValidateComponentPresence()
        {
            bool valid = true;
            if (_observationSources == null || _observationSources.Length == 0)
            { Debug.LogError("[EnemyBrain] No ObservationSource components.", this); valid = false; }
            if (_actionEffects == null || _actionEffects.Length == 0)
            { Debug.LogError("[EnemyBrain] No ActionEffect components.", this); valid = false; }
            return valid;
        }
    }
}

