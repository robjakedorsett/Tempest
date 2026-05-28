using System;

public class StateTransition<EState, TContext>
    where EState : Enum
{
    public EState From { get; }
    public EState To { get; }
    public Func<bool> Condition { get; }
    public Action OnTransition { get; set; }

    public StateTransition(EState from, EState to, Func<bool> condition, Action onTransition = null)
    {
        From = from;
        To = to;
        Condition = condition;
        OnTransition = onTransition;
    }
}
