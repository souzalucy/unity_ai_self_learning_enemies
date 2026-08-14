using System.Collections.Generic;
using Unity.MLAgents.Policies;
using UnityEngine;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>
    /// Configures a GameObject's ML-Agents BehaviorParameters to match the component
    /// stack's observation/action sizes and applies the selected experiment mode.
    /// Mirrors EnemyBrainEditor.AutoConfigureBP(), but at runtime.
    /// </summary>
    public static class BehaviorConfigurator
    {
        public static void Configure(GameObject go, MinigameSettings settings)
        {
            var bp = go.GetComponent<BehaviorParameters>();
            if (bp == null) return;

            var brainParameters = bp.BrainParameters;
            brainParameters.VectorObservationSize = SumObservationSize(go);
            var actionSpec = brainParameters.ActionSpec;
            actionSpec.NumContinuousActions = SumContinuousActions(go);
            actionSpec.BranchSizes = CollectBranchSizes(go);
            brainParameters.ActionSpec = actionSpec;

            ApplyExperimentMode(bp, settings);
        }

        private static int SumObservationSize(GameObject go)
        {
            int obsSize = 0;
            foreach (var o in go.GetComponents<ObservationSource>())
                if (o != null) obsSize += o.ObservationSize;
            return obsSize;
        }

        private static int SumContinuousActions(GameObject go)
        {
            int contTotal = 0;
            foreach (var a in go.GetComponents<ActionEffect>())
                if (a != null) contTotal += a.ContinuousActionCount;
            return contTotal;
        }

        private static int[] CollectBranchSizes(GameObject go)
        {
            var branchSizes = new List<int>();
            foreach (var a in go.GetComponents<ActionEffect>())
            {
                if (a == null) continue;
                if (a.DiscreteBranchSizes != null) branchSizes.AddRange(a.DiscreteBranchSizes);
            }
            return branchSizes.ToArray();
        }

        private static void ApplyExperimentMode(BehaviorParameters bp, MinigameSettings settings)
        {
            switch (settings.experimentMode)
            {
                case ExperimentMode.HeuristicOnly: bp.BehaviorType = BehaviorType.HeuristicOnly; break;
                case ExperimentMode.Training: bp.BehaviorType = BehaviorType.Default; break;
                case ExperimentMode.InferenceOnly: ApplyInferenceMode(bp, settings); break;
            }
        }

        private static void ApplyInferenceMode(BehaviorParameters bp, MinigameSettings settings)
        {
            bp.BehaviorType = BehaviorType.InferenceOnly;
            if (settings.inferenceModel != null) bp.Model = settings.inferenceModel;
            else Debug.LogWarning("[MinigameComposer] InferenceOnly selected but no model assigned.");
        }
    }
}
