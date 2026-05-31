using Tempest.Enemies.Enums;
using Tempest.Weapons;
using UnityEngine;

namespace Tempest.Enemies.Elite
{
    public class EliteAttackState : BaseState<EnemyStates, EnemyContext>
    {
        private enum Phase { Windup, Strike, Cooldown }

        private const float WindupDuration = 0.5f;
        private const float ConeHalfAngle = 30f;
        private const float TurnSpeed = 540f;

        private readonly Transform _transform;
        private readonly LayerMask _playerLayer;
        private readonly Renderer _renderer;
        private readonly MaterialPropertyBlock _propBlock;
        private readonly Color _originalColor;
        private readonly Color _telegraphColor = new(1f, 0.3f, 0f, 1f);

        private Phase _phase;
        private float _timer;
        private bool _attackComplete;

        public bool AttackComplete => _attackComplete;

        public EliteAttackState(StateMachine<EnemyStates, EnemyContext> stateMachine)
            : base(EnemyStates.Attack, stateMachine)
        {
            _transform = stateMachine.transform;
            _playerLayer = LayerConstants.Player;

            _renderer = stateMachine.GetComponentInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_propBlock);
                _originalColor = _propBlock.GetColor("_BaseColor");
                if (_originalColor == default)
                    _originalColor = _renderer.sharedMaterial.GetColor("_BaseColor");
            }
        }

        public override void EnterState()
        {
            base.EnterState();
            _attackComplete = false;
            Context.Agent.Stop();
            BeginWindup();
        }

        public override void FrameUpdate()
        {
            base.FrameUpdate();
            if (IsExitingState) return;

            if (Context.Target == null || Context.Target.IsDown)
            {
                _attackComplete = true;
                ResetTelegraph();
                return;
            }

            FaceTarget();

            _timer -= Time.deltaTime;

            switch (_phase)
            {
                case Phase.Windup:
                    UpdateTelegraph();
                    if (_timer <= 0f)
                        ExecuteStrike();
                    break;

                case Phase.Cooldown:
                    if (_timer <= 0f)
                        _attackComplete = true;
                    break;
            }
        }

        public override void ExitState()
        {
            ResetTelegraph();
            base.ExitState();
        }

        private void BeginWindup()
        {
            _phase = Phase.Windup;
            _timer = WindupDuration;
        }

        private void UpdateTelegraph()
        {
            if (_renderer == null) return;

            float t = 1f - (_timer / WindupDuration);
            Color current = Color.Lerp(_originalColor, _telegraphColor, t);
            _propBlock.SetColor("_BaseColor", current);
            _renderer.SetPropertyBlock(_propBlock);
        }

        private void ExecuteStrike()
        {
            ResetTelegraph();

            float attackRange = Context.Definition.attackRange;
            Collider[] hits = Physics.OverlapSphere(
                _transform.position, attackRange, _playerLayer);

            foreach (Collider hit in hits)
            {
                Vector3 dirToTarget = (hit.transform.position - _transform.position).normalized;
                float angle = Vector3.Angle(_transform.forward, dirToTarget);

                if (angle <= ConeHalfAngle)
                {
                    IDamageable damageable = hit.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        Vector3 hitPoint = hit.ClosestPoint(_transform.position);
                        Vector3 hitNormal = (_transform.position - hit.transform.position).normalized;
                        damageable.TakeDamage(Context.Definition.damage, hitPoint, hitNormal);
                    }
                }
            }

            _phase = Phase.Cooldown;
            _timer = Context.Definition.attackCooldown;
        }

        private void ResetTelegraph()
        {
            if (_renderer == null) return;
            _propBlock.SetColor("_BaseColor", _originalColor);
            _renderer.SetPropertyBlock(_propBlock);
        }

        private void FaceTarget()
        {
            Vector3 direction = Context.Target.transform.position - _transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                _transform.rotation = Quaternion.RotateTowards(
                    _transform.rotation, targetRot, TurnSpeed * Time.deltaTime);
            }
        }
    }
}
