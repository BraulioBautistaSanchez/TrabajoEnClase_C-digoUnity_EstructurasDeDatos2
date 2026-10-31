using System.Collections.Generic;
using UnityEngine;

// Construye la lista ligada con los quesos según el modo elegido en el menú
// y sabe cuál es el "siguiente objetivo" del agente.
public class PathManager : MonoBehaviour
{
    [Header("Aqui van los quesos")]
    [SerializeField] private Cheese[] cheeses;

    [Header("Orden del programador, modos Normal y Reversed")]
    [SerializeField] private CheeseType[] normalOrder =
    {
        CheeseType.LittleCheese,
        CheeseType.NormalCheese,
        CheeseType.AnormalCheese,
        CheeseType.BigCheese
    };

    private readonly MyLinkedList<Cheese> route = new MyLinkedList<Cheese>();

    private Node<Cheese> currentNode; 
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

        IEnumerable<CheeseType> order = normalOrder;

        if (GameSettings.Mode == RouteMode.Custom &&
            GameSettings.CustomOrder.Count == cheeses.Length)
        {
            order = GameSettings.CustomOrder;
        }

        foreach (CheeseType type in order)
        {
            Cheese cheese = FindCheese(type);
            if (cheese != null) route.AddLast(cheese);
            else Debug.LogWarning($"No encontré el queso {type} en la escena");
        }

        currentNode = reversed ? route.Tail : route.Head;
    }

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

}
