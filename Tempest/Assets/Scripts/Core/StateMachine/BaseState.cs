using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseState<EState, TContext>
    where EState : Enum
{
    protected Dictionary<EState, BaseState<EState, TContext>> SubStates
        = new();

    private readonly List<StateTransition<EState, TContext>> _transitions
        = new();

    protected StateMachine<EState, TContext> StateMachine;
    protected TContext Context => StateMachine.Context;

    public BaseState<EState, TContext> SubState { get; private set; }
    public BaseState<EState, TContext> SuperState { get; private set; }

    protected bool IsExitingState = false;

    public EState StateKey { get; }

    protected BaseState(EState stateKey, StateMachine<EState, TContext> stateMachine)
    {
        StateKey = stateKey;
        StateMachine = stateMachine;
    }

    public virtual void EnterState()
    {
        IsExitingState = false;
    }

    public virtual void ExitState()
    {
        SubState?.ExitState();
        SubState = null;
        IsExitingState = true;
    }

    public virtual void PhysicsUpdate()
    {
        SubState?.PhysicsUpdate();
    }

    public virtual void FrameUpdate()
    {
        foreach (var transition in _transitions)
        {
            if (transition.Condition != null && transition.Condition())
            {
                transition.OnTransition?.Invoke();

                if (SubStates.ContainsKey(transition.To))
                {
                    if (SubState != null && SubState.StateKey.Equals(transition.To))
                    {
                        continue;
                    }

                    SetSubState(transition.To);
                    return;
                }
                else
                {
                    StateMachine.ChangeState(transition.To);
                    return;
                }
            }
        }

        SubState?.FrameUpdate();
    }

    public virtual bool CanEnterState => true;
    public virtual bool AllowParentTransitions => true;

    public virtual void AddSubState(BaseState<EState, TContext> newSubState)
    {
        if (SubStates.ContainsKey(newSubState.StateKey))
        {
            Debug.LogWarning($"Duplicate sub-state: {newSubState.StateKey}");
            return;
        }
        newSubState.SetSuperState(this);
        SubStates.Add(newSubState.StateKey, newSubState);
    }

    public virtual void SetSubState(EState newSubStateKey)
    {
        if (SubState != null && SubState.StateKey.Equals(newSubStateKey))
            return;

        if (!SubStates.ContainsKey(newSubStateKey))
        {
            Debug.LogError($"Setting SubState failed: {newSubStateKey} not found as sub-state of {StateKey}");
            return;
        }

        var state = SubStates[newSubStateKey];
        if (!state.CanEnterState)
        {
            Debug.LogWarning($"Cannot enter sub-state {newSubStateKey} because CanEnterState is false");
            return;
        }

        if (SubState != null && !SubState.AllowParentTransitions)
        {
            Debug.LogWarning($"Cannot transition to sub-state {newSubStateKey} because current sub-state disallows it.");
            return;
        }

        SubState?.ExitState();
        SubState = state;
        SubState.EnterState();
    }

    public virtual void SetSuperState(BaseState<EState, TContext> newSuperState)
    {
        SuperState = newSuperState;
    }

    public TransitionBuilder<EState, TContext> FromThis()
    {
        return new TransitionBuilder<EState, TContext>(this);
    }

    public void AddTransition(StateTransition<EState, TContext> transition)
    {
        _transitions.Add(transition);
    }

    public virtual void OnTriggerEnter(Collider other)
    {
        SubState?.OnTriggerEnter(other);
    }

    public virtual void OnTriggerExit(Collider other)
    {
        SubState?.OnTriggerExit(other);
    }

    public virtual void OnTriggerStay(Collider other)
    {
        SubState?.OnTriggerStay(other);
    }
}
