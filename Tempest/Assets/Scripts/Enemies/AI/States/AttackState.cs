using Tempest.Enemies.Enums;
using UnityEngine;

namespace Tempest.Enemies
{
    public class AttackState : BaseState<EnemyStates, EnemyContext>
    {
        private float _attackTimer;

        public AttackState(StateMachine<EnemyStates, EnemyContext> stateMachine)
            : base(EnemyStates.Attack, stateMachine) { }

        public override void EnterState()
        {
            base.EnterState();
            Context.Agent.Stop();
            _attackTimer = 0f;
        }

        public override void FrameUpdate()
        {
            base.FrameUpdate();
            if (IsExitingState) return;

            if (Context.Target == null) return;

            FaceTarget();

            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                DealDamage();
                _attackTimer = Context.Definition.attackCooldown;
            }
        }

        private void FaceTarget()
        {
            Vector3 direction = Context.Target.transform.position - StateMachine.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                StateMachine.transform.rotation = Quaternion.RotateTowards(
                    StateMachine.transform.rotation,
                    targetRot,
                    540f * Time.deltaTime);
            }
        }

        private void DealDamage()
        {
            Vector3 hitDir = (Context.Target.transform.position - StateMachine.transform.position).normalized;
            Vector3 hitPoint = Context.Target.transform.position;

            Context.Target.TakeDamage(Context.Definition.damage, hitPoint, -hitDir);
        }
    }
}
