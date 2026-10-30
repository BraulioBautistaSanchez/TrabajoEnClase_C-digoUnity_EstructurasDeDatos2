using System.Collections.Generic;
using UnityEngine;

// Construye la lista ligada con los quesos según el modo elegido en el menú
// y sabe cuál es el "siguiente objetivo" del agente.
public class PathManager : MonoBehaviour
{
    [Header("Los 4 quesos de la escena (arrástralos en cualquier orden)")]
    [SerializeField] private Cheese[] cheeses;

    [Header("Orden del programador (modos Normal y Reversed)")]
    [SerializeField] private CheeseType[] normalOrder =
    {
        CheeseType.LittleCheese,
        CheeseType.NormalCheese,
        CheeseType.AnormalCheese,
        CheeseType.BigCheese
    };

    // ÚNICA lista ligada del proyecto
    private readonly MyLinkedList<Cheese> route = new MyLinkedList<Cheese>();

    private Node<Cheese> currentNode;   // en qué nodo de la lista vamos
    private bool reversed;

    public bool Finished => currentNode == null;
    public Cheese CurrentCheese => currentNode != null ? currentNode.Data : null;

    private void Awake()
    {
        BuildRoute();
    }

    private void BuildRoute()
    {
        route.Clear();
        reversed = GameSettings.Mode == RouteMode.Reversed;

        // 1) ¿Qué orden usamos para llenar la lista?
        IEnumerable<CheeseType> order = normalOrder;

        if (GameSettings.Mode == RouteMode.Custom &&
            GameSettings.CustomOrder.Count == cheeses.Length)
        {
            order = GameSettings.CustomOrder;
        }

        // 2) Insertamos un nodo por cada queso, al final de la lista
        foreach (CheeseType type in order)
        {
            Cheese cheese = FindCheese(type);
            if (cheese != null) route.AddLast(cheese);
            else Debug.LogWarning($"No encontré el queso {type} en la escena");
        }

        // 3) Empezamos en Head (normal / custom) o en Tail (invertido)
        currentNode = reversed ? route.Tail : route.Head;

        // 4) Mostramos la lista en la consola
        Debug.Log($"=== Modo: {GameSettings.Mode} ===");
        route.PrintToConsole(c => c.type.ToString());
    }

    // El agente llama esto al terminar de comerse un queso.
    // Ir hacia adelante = Next. Ir hacia atrás = Previous.
    public void Advance()
    {
        if (currentNode == null) return;
        currentNode = reversed ? currentNode.Previous : currentNode.Next;

        if (currentNode == null) Debug.Log("¡Ruta completada!");
        else Debug.Log($"Siguiente objetivo: {currentNode.Data.type}");
    }

    private Cheese FindCheese(CheeseType type)
    {
        foreach (Cheese c in cheeses)
            if (c.type == type) return c;
        return null;
    }

    // Texto en pantalla rápido para depurar sin montar UI
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 600, 25), $"Modo: {GameSettings.Mode}");
        string target = Finished ? "ninguno (terminó)" : CurrentCheese.type.ToString();
        GUI.Label(new Rect(10, 30, 600, 25), $"Objetivo actual: {target}");
    }
}
