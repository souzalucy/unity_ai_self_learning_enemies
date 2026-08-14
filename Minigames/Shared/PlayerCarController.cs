using UnityEngine;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>
    /// Human player car controller for the Racing minigame. Uses the exact same
    /// Rigidbody physics as ActionRigidBodyMovement so the comparison with the AI is fair.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerCarController : MonoBehaviour
    {
        [Header("Driving")]
        public float maxSteerAngle = 35f;
        public float motorForce = 1500f;
        public float brakeForce = 3000f;
        public float maxSpeed = 30f;

        [Header("Wheel Colliders (optional)")]
        public WheelCollider[] driveWheels;
        public WheelCollider[] steerWheels;

        private Rigidbody _rb;

        private void Awake()
        {
            gameObject.tag = "Player";
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            float steer = Input.GetAxisRaw("Horizontal");
            float accel = Mathf.Max(0f, Input.GetAxisRaw("Vertical"));
            float brake = Input.GetKey(KeyCode.Space) ? 1f : Mathf.Max(0f, -Input.GetAxisRaw("Vertical"));

            if (steerWheels != null && driveWheels != null && steerWheels.Length > 0)
            {
                foreach (var w in steerWheels) if (w != null) w.steerAngle = steer * maxSteerAngle;
                foreach (var w in driveWheels)
                {
                    if (w == null) continue;
                    w.motorTorque = accel * motorForce;
                    w.brakeTorque = brake * brakeForce;
                }
                return;
            }

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
