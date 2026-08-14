using UnityEngine;

namespace SelfLearningEnemies.Minigames
{
    /// <summary>
    /// Aim + fire action effect for the Shooter minigame. Consumes 2 continuous actions
    /// (aim yaw, aim pitch) and 1 discrete branch (0=none, 1=shoot, 2=reload, 3=grenade, 4=melee).
    /// Unlike ActionCombat, it uses raycasts with line-of-sight and registers hits/misses
    /// directly with RewardCombatPerformance.
    /// </summary>
    public class ActionAimAndShoot : ActionEffect
    {
        [Header("Aim")]
        [Tooltip("Transform that yaws/pitches with aim input. If null, the enemy's transform is rotated.")]
        public Transform aimPivot;

        [Tooltip("Degrees of yaw applied per decision at full input.")]
        public float yawStepPerDecision = 3f;

        [Tooltip("Degrees of pitch applied per decision at full input.")]
        public float pitchStepPerDecision = 2f;

        public float minPitch = -45f;
        public float maxPitch = 45f;

        [Header("Combat Slots")]
        [Tooltip("Slots: 0=shoot, 1=reload, 2=grenade, 3=melee (index 0 = none).")]
        [Range(1, 8)]
        public int combatSlotCount = 4;

        public float[] cooldowns = new float[] { 0.3f, 0.1f, 3f, 0.8f };

        [Header("Shoot")]
        public float shootDamage = 12f;
        public float shootRange = 40f;
        public float ammoCapacity = 30f;
        public float ammoPerShot = 1f;
        public float reloadTime = 2f;

        [Header("Grenade")]
        public float grenadeDamage = 30f;
        public float grenadeRadius = 4f;
        public float grenadeThrowDistance = 15f;

        [Header("Melee")]
        public float meleeDamage = 20f;
        public float meleeRange = 2.5f;

        [Header("Raycast")]
        public LayerMask hitMask = -1;

        [Tooltip("Ray origin. If null, uses aimPivot (or transform + eye height).")]
        public Transform muzzle;
        public float eyeHeight = 1.5f;

        private float _yaw;
        private float _pitch;
        private float _ammo;
        private bool _reloading;
        private float _reloadTimer;
        private float[] _cooldownTimers;
        private RewardCombatPerformance _reward;

        public override int DiscreteBranchCount => 1;
        public override int[] DiscreteBranchSizes => new int[] { combatSlotCount + 1 };
        public override int ContinuousActionCount => 2;

        private void Awake()
        {
            _cooldownTimers = new float[Mathf.Max(1, combatSlotCount)];
            _ammo = ammoCapacity;
            _reward = GetComponent<RewardCombatPerformance>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _cooldownTimers.Length; i++)
                if (_cooldownTimers[i] > 0f) _cooldownTimers[i] -= dt;

            if (_reloading)
            {
                _reloadTimer -= dt;
                if (_reloadTimer <= 0f)
                {
                    _reloading = false;
                    _ammo = ammoCapacity;
                }
            }
        }

        public override void ApplyActions(float[] discreteActions, float[] continuousActions)
        {
            ApplyAim(continuousActions);

            int slot = ResolveSlot(discreteActions);
            if (slot < 0) return;

            ExecuteSlot(slot);
            _cooldownTimers[slot] = cooldowns.Length > slot ? cooldowns[slot] : 1f;
        }

        private void ApplyAim(float[] continuousActions)
        {
            // Aim (2 continuous: yaw, pitch).
            float aimYaw = ReadContinuous(continuousActions, 0, -1f, 1f);
            float aimPitch = ReadContinuous(continuousActions, 1, -1f, 1f);
            _yaw += aimYaw * yawStepPerDecision;
            _pitch = Mathf.Clamp(_pitch + aimPitch * pitchStepPerDecision, minPitch, maxPitch);

            var pivot = aimPivot != null ? aimPivot : transform;
            pivot.rotation = Quaternion.Euler(-_pitch, _yaw, 0f);
        }

        private int ResolveSlot(float[] discreteActions)
        {
            int chosen = discreteActions.Length > 0 ? Mathf.RoundToInt(discreteActions[0]) : 0;
            if (chosen <= 0 || chosen > combatSlotCount) return -1;
            int slot = chosen - 1;
            if (slot >= _cooldownTimers.Length || _cooldownTimers[slot] > 0f) return -1;
            return slot;
        }

        private void ExecuteSlot(int slot)
        {
            switch (slot)
            {
                case 0: TryShoot(); break;
                case 1: StartReload(); break;
                case 2: TryGrenade(); break;
                case 3: TryMelee(); break;
            }
        }

        private static float ReadContinuous(float[] actions, int index, float min, float max) =>
            actions.Length > index ? Mathf.Clamp(actions[index], min, max) : 0f;

        private Vector3 MuzzlePosition()
        {
            if (muzzle != null) return muzzle.position;
            if (aimPivot != null) return aimPivot.position;
            return transform.position + Vector3.up * eyeHeight;
        }

        private Vector3 AimDirection()
        {
            return aimPivot != null ? aimPivot.forward : transform.forward;
        }

        /// <summary>
        /// Applies damage to an IDamageable or ICombatTarget on the hit component or its parents.
        /// Returns true when a target was found and damaged.
        /// </summary>
        private bool TryDamage(Component hitComponent, float damage)
        {
            var damageable = hitComponent.GetComponentInParent<IDamageable>();
            if (damageable != null) { damageable.TakeDamage(damage); return true; }

            var combatTarget = hitComponent.GetComponentInParent<ICombatTarget>();
            if (combatTarget != null) { combatTarget.TakeDamage(damage); return true; }

            return false;
        }

        private void TryShoot()
        {
            if (_reloading) return;
            if (_ammo <= 0f) { StartReload(); return; }
            _ammo = Mathf.Max(0f, _ammo - ammoPerShot);

            Vector3 origin = MuzzlePosition();
            Vector3 dir = AimDirection();

            if (Physics.Raycast(origin, dir, out RaycastHit hit, shootRange, hitMask))
            {
                if (TryDamage(hit.collider, shootDamage))
                    _reward?.RegisterHit(shootDamage);
                else
                    _reward?.RegisterMiss();
            }
            else
            {
                _reward?.RegisterMiss();
            }

            SoundEventManager.Emit(origin, 0.9f, SoundType.Gunshot);
        }

        private void StartReload()
        {
            if (_reloading || _ammo >= ammoCapacity) return;
            _reloading = true;
            _reloadTimer = reloadTime;
        }

        private void TryGrenade()
        {
            Vector3 origin = MuzzlePosition();
            Vector3 targetPos = origin + AimDirection() * grenadeThrowDistance;
            if (Physics.Raycast(origin, AimDirection(), out RaycastHit hit, grenadeThrowDistance, hitMask))
                targetPos = hit.point;

            bool hitAny = false;
            var hits = Physics.OverlapSphere(targetPos, grenadeRadius, hitMask);
            foreach (var h in hits)
            {
                if (TryDamage(h, grenadeDamage)) hitAny = true;
            }

            if (hitAny) _reward?.RegisterHit(grenadeDamage);
            else _reward?.RegisterMiss();

            SoundEventManager.Emit(targetPos, 1f, SoundType.Explosion);
        }

        private void TryMelee()
        {
            Vector3 origin = MuzzlePosition();
            if (Physics.Raycast(origin, AimDirection(), out RaycastHit hit, meleeRange, hitMask))
            {
                if (TryDamage(hit.collider, meleeDamage))
                    _reward?.RegisterHit(meleeDamage);
                else
                    _reward?.RegisterMiss();
            }
            else
            {
                _reward?.RegisterMiss();
            }
        }
    }
}

