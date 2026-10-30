using System;
using System.Text;
using UnityEngine;

// ---------------------------------------------------------------
// NODO: una "cajita" que guarda un dato y dos flechas (referencias):
//   Next     -> el nodo que sigue
//   Previous -> el nodo anterior
// ---------------------------------------------------------------
public class Node<T>
{
    public T Data;
    public Node<T> Next;
    public Node<T> Previous;

    public Node(T data)
    {
        Data = data;
        Next = null;
        Previous = null;
    }
}

// ---------------------------------------------------------------
// LISTA DOBLEMENTE LIGADA hecha desde cero (no usa System.Collections.Generic.LinkedList)
//
//  null <- [Head] <-> [ ] <-> [ ] <-> [Tail] -> null
//
// Solo guardamos Head y Tail; todo lo demás se alcanza "caminando"
// de nodo en nodo con Next / Previous.
// ---------------------------------------------------------------
public class MyLinkedList<T>
{
    public Node<T> Head { get; private set; }
    public Node<T> Tail { get; private set; }
    public int Count { get; private set; }

    // Agrega al final. Costo O(1) porque tenemos Tail.
    public void AddLast(T data)
    {
        Node<T> newNode = new Node<T>(data);

        if (Head == null)               // lista vacía: el nodo es Head y Tail a la vez
        {
            Head = newNode;
            Tail = newNode;
        }
        else
        {
            Tail.Next = newNode;        // el último actual apunta al nuevo
            newNode.Previous = Tail;    // el nuevo apunta de regreso
            Tail = newNode;             // el nuevo es ahora el último
        }
        Count++;
    }

    // Agrega al inicio. Costo O(1) porque tenemos Head.
    public void AddFirst(T data)
    {
        Node<T> newNode = new Node<T>(data);

        if (Head == null)
        {
            Head = newNode;
            Tail = newNode;
        }
        else
        {
            newNode.Next = Head;
            Head.Previous = newNode;
            Head = newNode;
        }
        Count++;
    }

    // Vacía la lista. Al perder las referencias, el recolector de basura limpia los nodos.
    public void Clear()
    {
        Head = null;
        Tail = null;
        Count = 0;
    }

    // Muestra la lista en la consola de Unity, de Head a Tail y de Tail a Head.
    // 'label' le dice cómo convertir cada dato en texto.
    public void PrintToConsole(Func<T, string> label)
    {
        if (Head == null)
        {
            Debug.Log("[Lista] vacía");
            return;
        }

        // Recorrido hacia adelante: seguimos las flechas Next
        StringBuilder forward = new StringBuilder("[Lista] Head -> ");
        Node<T> current = Head;
        int index = 0;
        while (current != null)
        {
            forward.Append($"({index}) {label(current.Data)} -> ");
            current = current.Next;
            index++;
        }
        forward.Append("null");
        Debug.Log(forward.ToString());

        // Recorrido hacia atrás: seguimos las flechas Previous
        StringBuilder backward = new StringBuilder("[Lista] Tail -> ");
        current = Tail;
        while (current != null)
        {
            backward.Append($"{label(current.Data)} -> ");
            current = current.Previous;
        }
        backward.Append("null");
        Debug.Log(backward.ToString());

        Debug.Log($"[Lista] Count = {Count}");
    }
}
