using Unity.MLAgents.Sensors;
using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Companion to ActionBehaviorTree. Reads the BT's suggested actions and feeds them
    /// as observations so the neural network learns when to trust vs. override the BT.
    ///
    /// Observation layout:
    ///   BT-suggested continuous values (same count as ActionBehaviorTree.continuousOutputs)
    ///   + BT active flag (1 float)
    ///   + BT status one-hot (3 floats: Success, Failure, Running)
    /// Total = continuousOutputs + 4
    /// </summary>
    [RequireComponent(typeof(ActionBehaviorTree))]
    public class ObsBehaviorTreeSuggestions : ObservationSource
    {
        private ActionBehaviorTree _bt;

        public override int ObservationSize
        {
            get
            {
                if (_bt == null) _bt = GetComponent<ActionBehaviorTree>();
                return _bt != null ? _bt.ContinuousActionCount + 4 : 4;
            }
        }

        private void Awake()
        {
            _bt = GetComponent<ActionBehaviorTree>();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            if (_bt == null) _bt = GetComponent<ActionBehaviorTree>();
            if (_bt == null)
            {
                // Pad with zeros
                for (int i = 0; i < 6; i++) sensor.AddObservation(0f);
                return;
            }

            // BT-suggested continuous actions
            float[] suggested = _bt.BTSuggestedContinuous;
            if (suggested != null)
            {
                foreach (float v in suggested)
                    sensor.AddObservation(Mathf.Clamp(v, -1f, 1f));
            }

            // BT active flag
            sensor.AddObservation(_bt.BTIsActive ? 1f : 0f);

            // BT status one-hot (Success, Failure, Running)
            var status = _bt.LastBTStatus;
            sensor.AddObservation(status == BT.BTStatus.Success ? 1f : 0f);
            sensor.AddObservation(status == BT.BTStatus.Failure ? 1f : 0f);
            sensor.AddObservation(status == BT.BTStatus.Running ? 1f : 0f);
        }
    }
}
