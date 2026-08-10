using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace SelfLearningEnemies
{
    [RequireComponent(typeof(BehaviorParameters))]
    [RequireComponent(typeof(DecisionRequester))]
    public class EnemyBrain : Agent
    {
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
        private int[] _discreteBranchSizes;
        private int[] _actionEffectDiscreteStart;
        private int[] _actionEffectContinuousStart;
        private float _episodeReward;
        private int _episodeStep;

        protected void Awake()
        {
            CacheComponents();
            ConfigureActionSpace();
        }

        public void CacheComponents()
        {
            _observationSources = GetComponents<ObservationSource>();
            _actionEffects = GetComponents<ActionEffect>();
            _rewardSources = GetComponents<RewardSource>();
        }

        private void ConfigureActionSpace()
        {
            var branchSizes = new List<int>();
            _actionEffectDiscreteStart = new int[_actionEffects.Length];
            int discreteIdx = 0;
            for (int i = 0; i < _actionEffects.Length; i++)
            {
                _actionEffectDiscreteStart[i] = discreteIdx;
                int[] sizes = _actionEffects[i].DiscreteBranchSizes;
                if (sizes != null)
                    foreach (int size in sizes) { branchSizes.Add(size); discreteIdx++; }
            }
            _discreteBranchSizes = branchSizes.ToArray();

            _actionEffectContinuousStart = new int[_actionEffects.Length];
            int contIdx = 0;
            for (int i = 0; i < _actionEffects.Length; i++)
            {
                _actionEffectContinuousStart[i] = contIdx;
                contIdx += _actionEffects[i].ContinuousActionCount;
            }
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
            int totalObs = 0;
            foreach (var src in sources)
            {
                if (src == null) continue;
                int before = sensor.ObservationSize();
                src.CollectObservations(sensor);
                int after = sensor.ObservationSize();
                int added = after - before;
                if (added != src.ObservationSize && src.ObservationSize > 0)
                    Debug.LogError($"[EnemyBrain] {src.SourceName} size mismatch: declared {src.ObservationSize}, added {added}", this);
                totalObs += src.ObservationSize;
            }
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            _episodeStep++;

            float[] discAll = actionBuffers.DiscreteActions.Array ?? Array.Empty<float>();
            float[] contAll = actionBuffers.ContinuousActions.Array ?? Array.Empty<float>();

            for (int i = 0; i < _actionEffects.Length; i++)
            {
                var eff = _actionEffects[i];
                if (eff == null) continue;

                int dStart = _actionEffectDiscreteStart[i];
                int dCount = eff.DiscreteBranchCount;
                float[] dSlice = new float[dCount];
                for (int j = 0; j < dCount; j++)
                    dSlice[j] = (dStart + j) < discAll.Length ? discAll[dStart + j] : 0f;

                int cStart = _actionEffectContinuousStart[i];
                int cCount = eff.ContinuousActionCount;
                float[] cSlice = new float[cCount];
                for (int j = 0; j < cCount; j++)
                    cSlice[j] = (cStart + j) < contAll.Length ? contAll[cStart + j] : 0f;

                eff.ApplyActions(dSlice, cSlice);
            }

            float stepReward = 0f;
            foreach (var rwd in _rewardSources)
            {
                if (rwd == null || !rwd.IsActive) continue;
                stepReward += rwd.CalculateReward() * rwd.RewardWeight;
            }

            if (genreProfile != null)
                stepReward += genreProfile.survivalRewardPerSecond * Time.fixedDeltaTime;

            AddReward(stepReward);
            _episodeReward += stepReward;
        }

        public override void Heuristic(in ActionBuffers actionBuffers)
        {
            var disc = actionBuffers.DiscreteActions;
            var cont = actionBuffers.ContinuousActions;

            float mx = 0f, mz = 0f;
            if (Input.GetKey(forwardKey)) mz += 1f;
            if (Input.GetKey(backKey)) mz -= 1f;
            if (Input.GetKey(rightKey)) mx += 1f;
            if (Input.GetKey(leftKey)) mx -= 1f;

            for (int i = 0; i < cont.Length; i++)
                cont[i] = i switch { 0 => mx, 1 => mz, _ => 0f };

            for (int i = 0; i < disc.Length; i++)
                disc[i] = 0;

            if (disc.Length > 0 && Input.GetKey(action1Key)) disc[0] = 1;
            if (disc.Length > 1 && Input.GetKey(action2Key)) disc[1] = 1;
            if (disc.Length > 2 && Input.GetKey(action3Key)) disc[2] = 1;
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
            bool valid = true;
            if (_observationSources == null || _observationSources.Length == 0)
            { Debug.LogError(\"[EnemyBrain] No ObservationSource components.\", this); valid = false; }
            if (_actionEffects == null || _actionEffects.Length == 0)
            { Debug.LogError(\"[EnemyBrain] No ActionEffect components.\", this); valid = false; }

            int obs = GetTotalObservationSize();
            int actSrc = _actionEffects?.Length ?? 0;
            int rwdSrc = _rewardSources?.Length ?? 0;
            Debug.Log($\"[EnemyBrain] Validation {(valid ? \"PASSED\" : \"FAILED\")}: {obs} obs, {actSrc} actions, {rwdSrc} rewards.\");
            return valid;
        }
    }
}

