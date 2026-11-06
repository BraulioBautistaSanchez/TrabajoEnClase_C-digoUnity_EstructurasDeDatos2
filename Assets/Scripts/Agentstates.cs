using UnityEngine;
// Clase base para los estados del agente.
public abstract class AgentState : IState
{
    protected readonly SteeringAgent agent;

    protected AgentState(SteeringAgent agent)
    {
        this.agent = agent;
    }

    public virtual void Enter() { }
    public abstract void Tick();
    public virtual void Exit() { }

    // Atajo para pedir una transición
    protected void GoTo(IState next)
    {
        agent.Fsm.ChangeState(next);
    }
}

// IDLE: el agente espera un momento al iniciar la escena.
public class IdleState : AgentState
{
    private float timer;

    public IdleState(SteeringAgent agent) : base(agent) { }

    public override void Enter()
    {
        agent.Stop();
        timer = agent.IdleTime;
    }

    public override void Tick()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        GoTo(agent.Path.Finished ? agent.StateFinished : agent.StateMoving);
    }
}

// MOVING: va hacia el queso actual (Path Following + Arrival).

public class MovingState : AgentState
{
    public MovingState(SteeringAgent agent) : base(agent) { }

    public override void Tick()
    {
        if (agent.Path.Finished)
        {
            GoTo(agent.StateFinished);
            return;
        }

        if (agent.HasReachedTarget())
        {
            GoTo(agent.StateEating);
            return;
        }

        if (agent.TryDetectObstacle(out _))
        {
            GoTo(agent.StateAvoiding);
            return;
        }

        agent.ApplySteering(agent.ComputeArrive());
    }
}

// AVOIDING: hay un obstáculo entre el agente y el queso; el esquive tiene prioridad.
public class AvoidingState : AgentState
{
    private float clearTimer;   // cuánto tiempo lleva el camino libre

    public AvoidingState(SteeringAgent agent) : base(agent) { }

    public override void Enter()
    {
        clearTimer = 0f;
    }

    public override void Tick()
    {
        if (agent.HasReachedTarget())
        {
            GoTo(agent.StateEating);
            return;
        }

        if (agent.TryDetectObstacle(out RaycastHit2D hit))
        {
            // PRIORIDAD AL ESQUIVE: mientras vea el obstáculo, ignora el Arrival.
            // (Si se sumaran, las dos fuerzas se cancelarían de frente.)
            clearTimer = 0f;
            agent.ApplySteering(agent.ComputeAvoid(hit));
        }
        else
        {
            // Camino libre: avanza hacia el queso y cuenta el tiempo para volver a Moving
            clearTimer += Time.deltaTime;
            if (clearTimer >= agent.AvoidClearTime)
            {
                GoTo(agent.StateMoving);
                return;
            }
            agent.ApplySteering(agent.ComputeArrive());
        }
    }
}

// EATING: se come el queso (y avanza al siguiente nodo de la lista).

public class EatingState : AgentState
{
    private float timer;

    public EatingState(SteeringAgent agent) : base(agent) { }

    public override void Enter()
    {
        agent.Stop();
        agent.EatCurrentCheese();   // oculta el queso y hace path.Advance()
        timer = agent.EatPause;
    }

    public override void Tick()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        GoTo(agent.Path.Finished ? agent.StateFinished : agent.StateMoving);
    }
}

// FINISHED: ya pasó por todos los quesos y espera.
public class FinishedState : AgentState
{
    public FinishedState(SteeringAgent agent) : base(agent) { }

    public override void Enter()
    {
        agent.Stop();
        Debug.Log("¡El agente terminó la ruta!");
    }

    // Si aparece un queso nuevo en la lista, el agente reanuda la búsqueda
    public override void Tick()
    {
        if (!agent.Path.Finished)
            GoTo(agent.StateMoving);
    }
}
