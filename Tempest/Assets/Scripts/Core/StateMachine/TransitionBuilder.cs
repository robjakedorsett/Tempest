using System;

public class TransitionBuilder<EState, TContext>
    where EState : Enum
{
    private readonly BaseState<EState, TContext> _state;
    private EState _to;
    private Func<bool> _condition;
    private Action _onTransition;

    public TransitionBuilder(BaseState<EState, TContext> state)
    {
        _state = state;
    }

    public TransitionBuilder<EState, TContext> To(EState to)
    {
        _to = to;
        return this;
    }

    public TransitionBuilder<EState, TContext> When(Func<bool> condition)
    {
        _condition = condition;
        return this;
    }

    public TransitionBuilder<EState, TContext> OnTransition(Action callback)
    {
        _onTransition = callback;
        return this;
    }

    public void Build()
    {
        var transition = new StateTransition<EState, TContext>(
            _state.StateKey,
            _to,
            _condition,
            _onTransition
        );
        _state.AddTransition(transition);
    }
}
