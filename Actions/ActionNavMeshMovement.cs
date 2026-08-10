using UnityEngine;
using UnityEngine.AI;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Moves the enemy using NavMeshAgent from continuous actions.
    /// 2 continuous floats: [moveX, moveZ] mapped to a world-space direction delta from current position.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ActionNavMeshMovement : ActionEffect
    {
        [Tooltip("Movement speed multiplier.")]
        public float moveSpeed = 5f;

        [Tooltip("How far ahead to set the destination (in world units).")]
        public float destinationDistance = 3f;

        private NavMeshAgent _agent;

        public override int DiscreteBranchCount => 0;
        public override int[] DiscreteBranchSizes => new int[0];
        public override int ContinuousActionCount => 2;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = moveSpeed;
        }

        public override void ApplyActions(float[] discreteActions, float[] continuousActions)
        {
            if (_agent == null || !_agent.isOnNavMesh) return;

            float moveX = continuousActions.Length > 0 ? continuousActions[0] : 0f;
            float moveZ = continuousActions.Length > 1 ? continuousActions[1] : 0f;

            // Treat input as a direction in the XZ plane
            Vector3 inputDir = new Vector3(moveX, 0f, moveZ);
            if (inputDir.magnitude > 1f) inputDir.Normalize();

            if (inputDir.magnitude < 0.1f)
            {
                _agent.isStopped = true;
                return;
            }

            _agent.isStopped = false;
            Vector3 dest = transform.position + inputDir * destinationDistance;

            // Clamp to NavMesh
            if (NavMesh.SamplePosition(dest, out NavMeshHit hit, destinationDistance * 1.5f, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
            else
                _agent.SetDestination(transform.position);
        }
    }
}
