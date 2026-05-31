using Tempest.Enemies.Enums;
using UnityEngine;

namespace Tempest.Enemies.Elite
{
    public class EliteChaseState : BaseState<EnemyStates, EnemyContext>
    {
        private const float LostTargetDuration = 5f;

        private readonly Transform _transform;
        private float _lostTimer;
        private bool _targetLost;
        private Vector3 _approachOffset;

        public bool TargetLost => _targetLost;
        public bool InAttackRange { get; private set; }

        public EliteChaseState(StateMachine<EnemyStates, EnemyContext> stateMachine)
            : base(EnemyStates.Chase, stateMachine)
        {
            _transform = stateMachine.transform;
        }

        public override void EnterState()
        {
            base.EnterState();
            _lostTimer = 0f;
            _targetLost = false;
            InAttackRange = false;
            GenerateApproachOffset();

            Context.Agent.Initialize(Context.Definition.moveSpeed);
        }

        public override void FrameUpdate()
        {
            base.FrameUpdate();
            if (IsExitingState) return;

            if (Context.Target == null || Context.Target.IsDown)
            {
                RetargetOrLose();
                return;
            }

            Vector3 targetPos = Context.Target.transform.position;
            Context.Agent.SetDestination(targetPos + _approachOffset);

            float dist = Vector3.Distance(_transform.position, targetPos);
            InAttackRange = dist <= Context.Definition.attackRange;

            if (dist > Context.Definition.detectionRadius)
            {
                _lostTimer += Time.deltaTime;
                if (_lostTimer >= LostTargetDuration)
                {
                    _targetLost = true;
                    PlayerRegistry.ReleaseTarget(Context.Target);
                    Context.Target = null;
                }
            }
            else
            {
                _lostTimer = 0f;
            }
        }

        public override void ExitState()
        {
            base.ExitState();
            Context.Agent.Stop();
        }

        private void RetargetOrLose()
        {
            PlayerHealth newTarget = PlayerRegistry.GetBestTarget(_transform.position);
            if (newTarget != null)
            {
                Context.Target = newTarget;
                PlayerRegistry.AssignTarget(newTarget);
                GenerateApproachOffset();
                _lostTimer = 0f;
            }
            else
            {
                _targetLost = true;
                Context.Target = null;
            }
        }

        private void GenerateApproachOffset()
        {
            float radius = Context.Definition.attackRange * 0.6f;
            Vector2 circle = Random.insideUnitCircle * radius;
            _approachOffset = new Vector3(circle.x, 0f, circle.y);
        }
    }
}
