using System;
using System.Text;
using UnityEngine;

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

public class MyLinkedList<T>
{
    public Node<T> Head { get; private set; }
    public Node<T> Tail { get; private set; }
    public int Count { get; private set; }


    public void AddLast(T data)
    {
        Node<T> newNode = new Node<T>(data);

        if (Head == null)
        {
            Head = newNode;
            Tail = newNode;
        }
        else
        {
            Tail.Next = newNode;
            newNode.Previous = Tail;
            Tail = newNode;
        }
        Count++;
    }

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


    public void Clear()
    {
        Head = null;
        Tail = null;
        Count = 0;
    }


}