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
            SetActionKey(disc, 0, action1Key);
            SetActionKey(disc, 1, action2Key);
            SetActionKey(disc, 2, action3Key);
        }

        private static void SetActionKey(ActionSegment<int> disc, int index, KeyCode key)
        {
            if (disc.Length > index && Input.GetKey(key)) disc[index] = 1;
        }
    }
}