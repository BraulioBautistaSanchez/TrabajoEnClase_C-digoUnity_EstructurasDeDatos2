using UnityEngine;

// La máquina de estados: solo recuerda cuál es el estado actua y se encarga de cambiarlo de forma ordenada.
public class StateMachine
{
    public IState Current { get; private set; }

    public void ChangeState(IState next)
    {
        if (next == null || next == Current) return;

        string from = Current == null ? "ninguno" : Current.GetType().Name;
        Debug.Log($"[FSM] {from} -> {next.GetType().Name}");

        Current?.Exit();    // 1) el estado viejo se despide
        Current = next;     // 2) cambiamos el estado actual
        Current.Enter();    // 3) el estado nuevo se prepara
    }

    // Se llama cada frame desde el Update del dueño de la máquina
    public void Tick()
    {
        Current?.Tick();
    }
}
