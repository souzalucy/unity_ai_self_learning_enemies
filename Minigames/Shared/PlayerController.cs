using UnityEngine;
using UnityEngine.AI;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>
    /// Human player controller for RPG and Shooter minigames.
    /// NavMesh movement (WASD), mouse aim, and a hitscan fire button.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 6f;

        [Header("Aim")]
        public float aimYawSensitivity = 180f;
        public float aimPitchSensitivity = 120f;
        public float minPitch = -60f;
        public float maxPitch = 60f;
        public float cameraDistance = 5f;
        public float cameraHeight = 2f;

        [Header("Fire / Use")]
        public float fireRange = 50f;
        public float fireDamage = 15f;
        public float fireRate = 0.15f;
        public LayerMask fireMask = -1;
        public Transform muzzle;

        [Tooltip("Self-heal applied when pressing the 'Use' key (E). 0 disables.")]
        public float healOnUse = 25f;
        public float healCooldown = 4f;

        private Camera _cam;
        private NavMeshAgent _agent;
        private float _yaw;
        private float _pitch;
        private float _fireCooldown;
        private float _healCooldown;

        private void Awake()
        {
            gameObject.tag = "Player";
            _agent = GetComponent<NavMeshAgent>();
            if (_agent != null)
            {
                _agent.updateRotation = false;
                _agent.speed = moveSpeed;
            }
        }

        private void Start()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                _cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            _yaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            HandleAim();
            HandleMovement();
            HandleFire();
            HandleUse();
            UpdateCamera();

            _fireCooldown -= Time.deltaTime;
            _healCooldown -= Time.deltaTime;
        }

        private void HandleAim()
        {
            _yaw += Input.GetAxis("Mouse X") * aimYawSensitivity * Time.deltaTime;
            _pitch = Mathf.Clamp(_pitch - Input.GetAxis("Mouse Y") * aimPitchSensitivity * Time.deltaTime, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        private void HandleMovement()
        {
            float mx = Input.GetAxisRaw("Horizontal");
            float mz = Input.GetAxisRaw("Vertical");

            Vector3 camFwd = _cam != null ? Vector3.ProjectOnPlane(_cam.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 camRight = _cam != null ? Vector3.ProjectOnPlane(_cam.transform.right, Vector3.up).normalized : Vector3.right;
            Vector3 move = camFwd * mz + camRight * mx;
            if (move.sqrMagnitude > 1f) move.Normalize();
            move *= moveSpeed;

            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = move.sqrMagnitude < 0.01f;
                _agent.velocity = move;
            }
            else
            {
                transform.position += move * Time.deltaTime;
            }
        }

        private void HandleFire()
        {
            if (_fireCooldown > 0f || !Input.GetButton("Fire1")) return;
            _fireCooldown = fireRate;

            // Fire from the body (inside the player collider) so the player doesn't shoot itself.
            Vector3 origin = FireOrigin();
            Vector3 dir = FireDirection();
            DealHitscanDamage(origin, dir);
            SoundEventManager.Emit(origin, 0.8f, SoundType.Gunshot);
        }

        private Vector3 FireOrigin() =>
            muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.5f;

        private Vector3 FireDirection() =>
            _cam != null ? _cam.transform.forward : transform.forward;

        private void DealHitscanDamage(Vector3 origin, Vector3 dir)
        {
            if (!Physics.Raycast(origin, dir, out RaycastHit hit, fireRange, fireMask)) return;
            var target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null) target.TakeDamage(fireDamage);
            else hit.collider.GetComponentInParent<ICombatTarget>()?.TakeDamage(fireDamage);
        }

        private void HandleUse()
        {
            if (healOnUse <= 0f || _healCooldown > 0f || !Input.GetKeyDown(KeyCode.E)) return;
            _healCooldown = healCooldown;
            var status = GetComponent<SimpleStatusProvider>();
            if (status != null) status.Heal(healOnUse);
        }

        private void UpdateCamera()
        {
            if (_cam == null) return;
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            _cam.transform.rotation = rot;
            _cam.transform.position = transform.position + Vector3.up * cameraHeight - rot * Vector3.forward * cameraDistance;
        }
    }
}
