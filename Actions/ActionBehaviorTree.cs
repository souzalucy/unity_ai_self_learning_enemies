using SelfLearningEnemies.BT;
using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Hybrid action effect that runs a Behavior Tree for hand-crafted logic while
    /// allowing the neural network to override BT decisions.
    ///
    /// Architecture:
    ///   1 discrete branch: 0=FollowBT (use BT-suggested actions), 1=OverrideBT (use learned actions)
    ///   N continuous actions: actual movement/aim parameters
    ///
    /// The BT runs every step and writes suggested actions to a public buffer.
    /// Pair with ObsBehaviorTreeSuggestions to feed BT intent into the network's observations.
    /// </summary>
    public class ActionBehaviorTree : ActionEffect
    {
        [Header("BT Root")]
        [Tooltip("The root behavior tree node. Build your tree in code or via a builder.")]
        public string behaviorTreeName = "DefaultBT";

        [Header("Action Output")]
        [Tooltip("Number of continuous actions this effect controls (e.g., 2 for movement).")]
        [Range(0, 8)]
        public int continuousOutputs = 2;

        [Header("Blending")]
        [Tooltip("When FollowBT is active, blend weight between BT(0) and learned(1). 0 = pure BT.")]
        [Range(0f, 1f)]
        public float btBlend = 0f;

        // Public buffer that ObsBehaviorTreeSuggestions reads from
        public float[] BTSuggestedContinuous { get; private set; }
        public float BTSuggestedDiscrete { get; private set; }
        public bool BTIsActive { get; private set; }
        public BTStatus LastBTStatus { get; private set; } = BTStatus.Failure;

        // The BT root — set this from code or a builder component
        [System.NonSerialized]
        public BTNode rootNode;

        // Callback for when BT wants to execute an action
        public System.Action<string> onBTDebugLog;

        public override int DiscreteBranchCount => 1;
        public override int[] DiscreteBranchSizes => new int[] { 2 }; // FollowBT, OverrideBT
        public override int ContinuousActionCount => continuousOutputs;

        private void Awake()
        {
            BTSuggestedContinuous = new float[continuousOutputs];
        }

        /// <summary>
        /// Build a default BT for testing. Override in your own setup.
        /// </summary>
        public void BuildDefaultPatrolBT(Transform[] patrolPoints)
        {
            // Simple patrol: move toward nearest patrol point, attack if enemy nearby
            // This is a placeholder — users should build their own trees
            rootNode = new BTSequence("Root",
                new BTActionNode(() => onBTDebugLog?.Invoke("BT: Patrolling"), BTStatus.Success, "Patrol")
            );
        }

        /// <summary>
        /// Build a default combat BT.
        /// </summary>
        public void BuildDefaultCombatBT(Transform target)
        {
            rootNode = new BTSelector("CombatRoot",
                new BTSequence("AttackSeq",
                    new BTCondition(() => target != null && Vector3.Distance(transform.position, target.position) < 15f, "EnemyInRange"),
                    new BTActionNode(() =>
                    {
                        // Set BT-suggested actions: move toward target
                        if (target != null)
                        {
                            Vector3 dir = (target.position - transform.position).normalized;
                            BTSuggestedContinuous[0] = dir.x;
                            if (BTSuggestedContinuous.Length > 1)
                                BTSuggestedContinuous[1] = dir.z;
                        }
                        BTSuggestedDiscrete = 0f;
                        BTIsActive = true;
                    }, BTStatus.Success, "Attack")
                ),
                new BTActionNode(() =>
                {
                    // Idle / patrol
                    BTSuggestedContinuous[0] = 0f;
                    if (BTSuggestedContinuous.Length > 1)
                        BTSuggestedContinuous[1] = 0f;
                    BTSuggestedDiscrete = 0f;
                    BTIsActive = false;
                }, BTStatus.Success, "Idle")
            );
        }

        public override void ApplyActions(float[] discreteActions, float[] continuousActions)
        {
            TickTree();

            // Determine mode: FollowBT (0) or OverrideBT (1)
            bool overrideBT = ResolveOverride(discreteActions);

            // Follow BT: use BT-suggested actions, optionally blended with learned actions.
            // If OverrideBT, the learned continuous actions are used directly by the caller.
            if (!overrideBT && rootNode != null)
                BlendWithBT(continuousActions);
        }

        private void TickTree()
        {
            if (rootNode == null) return;

            // Reset BT suggestions
            for (int i = 0; i < BTSuggestedContinuous.Length; i++)
                BTSuggestedContinuous[i] = 0f;
            BTSuggestedDiscrete = 0f;
            BTIsActive = false;

            LastBTStatus = rootNode.Tick();
        }

        private static bool ResolveOverride(float[] discreteActions)
        {
            int mode = discreteActions.Length > 0 ? Mathf.RoundToInt(discreteActions[0]) : 0;
            return mode == 1;
        }

        private void BlendWithBT(float[] continuousActions)
        {
            for (int i = 0; i < continuousOutputs && i < continuousActions.Length; i++)
            {
                float btVal = i < BTSuggestedContinuous.Length ? BTSuggestedContinuous[i] : 0f;
                float learnedVal = continuousActions[i];
                BTSuggestedContinuous[i] = Mathf.Lerp(btVal, learnedVal, btBlend);
            }
        }
    }
}
