// Contrato que debe cumplir CUALQUIER estado.
// La máquina de estados solo conoce esta interfaz, no los estados concretos.
public interface IState
{
    void Enter();   // se ejecuta UNA vez al entrar al estado
    void Tick();    // se ejecuta CADA frame mientras el estado esté activo
    void Exit();    // se ejecuta UNA vez al salir del estado
}
