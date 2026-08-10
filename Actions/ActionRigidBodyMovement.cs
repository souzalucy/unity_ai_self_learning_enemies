using UnityEngine;

namespace SelfLearningEnemies
{
    /// <summary>
    /// Racing-style movement via Rigidbody forces.
    /// 3 continuous floats: [steer(-1..1), accelerate(0..1), brake(0..1)]
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ActionRigidBodyMovement : ActionEffect
    {
        [Header("Driving Params")]
        public float maxSteerAngle = 35f;
        public float motorForce = 1500f;
        public float brakeForce = 3000f;
        public float maxSpeed = 30f;

        [Header("Wheel Colliders (optional)")]
        public WheelCollider[] driveWheels;
        public WheelCollider[] steerWheels;

        private Rigidbody _rb;

        public override int DiscreteBranchCount => 0;
        public override int[] DiscreteBranchSizes => new int[0];
        public override int ContinuousActionCount => 3;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public override void ApplyActions(float[] discreteActions, float[] continuousActions)
        {
            float steer = continuousActions.Length > 0 ? Mathf.Clamp(continuousActions[0], -1f, 1f) : 0f;
            float accel = continuousActions.Length > 1 ? Mathf.Clamp(continuousActions[1], 0f, 1f) : 0f;
            float brake = continuousActions.Length > 2 ? Mathf.Clamp(continuousActions[2], 0f, 1f) : 0f;

            // Apply via WheelColliders if assigned
            if (steerWheels != null && driveWheels != null && steerWheels.Length > 0)
            {
                foreach (var w in steerWheels)
                {
                    if (w != null) w.steerAngle = steer * maxSteerAngle;
                }
                foreach (var w in driveWheels)
                {
                    if (w != null)
                    {
                        w.motorTorque = accel * motorForce;
                        w.brakeTorque = brake * brakeForce;
                    }
                }
                return;
            }

            // Fallback: apply directly to Rigidbody
            if (_rb == null) return;

            float speed = _rb.linearVelocity.magnitude;
            if (speed < maxSpeed && accel > 0.01f)
                _rb.AddForce(transform.forward * accel * motorForce * Time.fixedDeltaTime, ForceMode.Acceleration);

            if (brake > 0.01f)
                _rb.AddForce(-_rb.linearVelocity.normalized * brake * brakeForce * Time.fixedDeltaTime, ForceMode.Acceleration);

            if (Mathf.Abs(steer) > 0.01f && speed > 0.5f)
            {
                float turn = steer * maxSteerAngle * Mathf.Deg2Rad;
                _rb.AddTorque(transform.up * turn * motorForce * 0.5f * Time.fixedDeltaTime, ForceMode.Acceleration);
            }
        }
    }
}
