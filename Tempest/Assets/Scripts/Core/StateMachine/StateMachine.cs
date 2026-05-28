using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class StateMachine<EState, TContext> : MonoBehaviour
    where EState : Enum
{
    protected Dictionary<EState, BaseState<EState, TContext>> States
        = new();

    public BaseState<EState, TContext> CurrentState;
    public TContext Context;

    public void Initialize(TContext context)
    {
        Context = context;
    }

    public void SetInitialState(EState stateKey)
    {
        if (!States.ContainsKey(stateKey))
        {
            Debug.LogError($"Setting Initial State Failed! - State not found: {stateKey}");
            return;
        }

        CurrentState = States[stateKey];
        CurrentState.EnterState();
    }

    public void AddState(BaseState<EState, TContext> state)
    {
        if (States.ContainsKey(state.StateKey))
        {
            Debug.LogError($"Adding State Failed! - State key already registered: {state.StateKey}");
            return;
        }
        States.Add(state.StateKey, state);
    }

    public void ChangeState(EState stateKey)
    {
        if (!States.ContainsKey(stateKey))
        {
            Debug.LogError($"Changing State Failed! - State not found: {stateKey}");
            return;
        }

        if (CurrentState == States[stateKey]) return;

        CurrentState?.ExitState();
        CurrentState = States[stateKey];
        CurrentState.EnterState();
    }

    protected virtual void Awake()
    {
        if (Context == null)
        {
            Context = GetComponent<TContext>();
            if (Context == null)
                Debug.LogError($"Context {nameof(TContext)} not found on {gameObject.name}");
        }
    }

    protected virtual void Update()
    {
        CurrentState?.FrameUpdate();
    }

    protected virtual void FixedUpdate()
    {
        CurrentState?.PhysicsUpdate();
    }

    private void OnTriggerEnter(Collider other)
    {
        CurrentState?.OnTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        CurrentState?.OnTriggerExit(other);
    }

    private void OnTriggerStay(Collider other)
    {
        CurrentState?.OnTriggerStay(other);
    }

    public string DebugStatePath
    {
        get
        {
            if (CurrentState == null) return "<none>";

            var s = CurrentState;
            System.Text.StringBuilder sb = new();
            sb.Append(s.StateKey.ToString());

            while (s.SubState != null)
            {
                s = s.SubState;
                sb.Append(" / ");
                sb.Append(s.StateKey.ToString());
            }

            return sb.ToString();
        }
    }
}
