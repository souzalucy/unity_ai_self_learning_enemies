using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;

namespace SelfLearningEnemies
{
    public partial class EnemyBrain
    {
        public override void Heuristic(in ActionBuffers actionBuffers)
        {
            var disc = actionBuffers.DiscreteActions;
            var cont = actionBuffers.ContinuousActions;

            float mx = GetHeuristicHorizontal();
            float mz = GetHeuristicVertical();
            for (int i = 0; i < cont.Length; i++)
                cont[i] = i switch { 0 => mx, 1 => mz, _ => 0f };

            for (int i = 0; i < disc.Length; i++)
                disc[i] = 0;
            ApplyHeuristicDiscrete(disc);
        }

        private float GetHeuristicHorizontal()
        {
            float mx = 0f;
            if (Input.GetKey(rightKey)) mx += 1f;
            if (Input.GetKey(leftKey)) mx -= 1f;
            return mx;
        }

        private float GetHeuristicVertical()
        {
            float mz = 0f;
            if (Input.GetKey(forwardKey)) mz += 1f;
            if (Input.GetKey(backKey)) mz -= 1f;
            return mz;
        }

        private void ApplyHeuristicDiscrete(ActionSegment<int> disc)
        {
            if (disc.Length > 0 && Input.GetKey(action1Key)) disc[0] = 1;
            if (disc.Length > 1 && Input.GetKey(action2Key)) disc[1] = 1;
            if (disc.Length > 2 && Input.GetKey(action3Key)) disc[2] = 1;
        }
    }
}