using System.Collections.Generic;
using UnityEngine;

// Construye la lista ligada con los quesos según el modo elegido en el menú,
// sabe cuál es el "siguiente objetivo" del agente y permite agregar quesos nuevos.
public class PathManager : MonoBehaviour
{
    [Header("Los 4 quesos de la escena (arrástralos en cualquier orden)")]
    [SerializeField] private Cheese[] cheeses;

    [Header("Orden del programador (modos Normal y Reversed)")]
    [SerializeField]
    private CheeseType[] normalOrder =
    {
        CheeseType.LittleCheese,
        CheeseType.NormalCheese,
        CheeseType.AnormalCheese,
        CheeseType.BigCheese
    };

    // ÚNICA lista ligada del proyecto
    private readonly MyLinkedList<Cheese> route = new MyLinkedList<Cheese>();

    // Registro de TODOS los quesos que existen (los 4 iniciales + los extra).
    // No es la lista ligada: solo sirve para que el spawner sepa dónde están.
    private readonly List<Cheese> allCheeses = new List<Cheese>();

    private Node<Cheese> currentNode;   // en qué nodo de la lista vamos
    private bool reversed;

    public bool Finished => currentNode == null;
    public Cheese CurrentCheese => currentNode != null ? currentNode.Data : null;
    public IReadOnlyList<Cheese> AllCheeses => allCheeses;

    private void Awake()
    {
        allCheeses.AddRange(cheeses);
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
        route.PrintToConsole(c => c.Label);
    }

    // Agrega un queso NUEVO a la lista mientras el juego corre.
    // Siempre queda al FINAL del recorrido:
    //   - modos Normal / Custom (recorren con Next)    -> AddLast  (nuevo Tail)
    //   - modo Reversed (recorre con Previous)         -> AddFirst (nuevo Head)
    public void AddCheese(Cheese cheese)
    {
        allCheeses.Add(cheese);

        if (reversed) route.AddFirst(cheese);
        else route.AddLast(cheese);

        // Si el agente ya había terminado (currentNode == null), reanuda en el nuevo nodo
        if (currentNode == null)
            currentNode = reversed ? route.Head : route.Tail;

        Debug.Log($"[Lista] Queso nuevo agregado: {cheese.Label}");
        route.PrintToConsole(c => c.Label);
    }

    // El agente llama esto al terminar de comerse un queso.
    // Ir hacia adelante = Next. Ir hacia atrás = Previous.
    public void Advance()
    {
        if (currentNode == null) return;
        currentNode = reversed ? currentNode.Previous : currentNode.Next;

        if (currentNode == null) Debug.Log("¡Ruta completada!");
        else Debug.Log($"Siguiente objetivo: {currentNode.Data.Label}");
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
        string target = Finished ? "ninguno (terminó)" : CurrentCheese.Label;
        GUI.Label(new Rect(10, 30, 600, 25), $"Objetivo actual: {target}");
    }
}