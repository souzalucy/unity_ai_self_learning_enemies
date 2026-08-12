using System;
using System.Collections.Generic;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Action space configuration and action dispatch to effects.
    /// </summary>
    public partial class EnemyBrain
    {
        private int[] _discreteBranchSizes;

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

        private void DispatchActions(float[] discAll, float[] contAll)
        {
            for (int i = 0; i < _actionEffects.Length; i++)
            {
                var eff = _actionEffects[i];
                if (eff == null) continue;
                float[] dSlice = SliceDiscrete(discAll, _actionEffectDiscreteStart[i], eff.DiscreteBranchCount);
                float[] cSlice = SliceContinuous(contAll, _actionEffectContinuousStart[i], eff.ContinuousActionCount);
                eff.ApplyActions(dSlice, cSlice);
            }
        }

        private static float[] SliceDiscrete(float[] src, int start, int count)
        {
            float[] result = new float[count];
            for (int j = 0; j < count; j++)
                result[j] = (start + j) < src.Length ? src[start + j] : 0f;
            return result;
        }

        private static float[] SliceContinuous(float[] src, int start, int count)
        {
            float[] result = new float[count];
            for (int j = 0; j < count; j++)
                result[j] = (start + j) < src.Length ? src[start + j] : 0f;
            return result;
        }
    }
}