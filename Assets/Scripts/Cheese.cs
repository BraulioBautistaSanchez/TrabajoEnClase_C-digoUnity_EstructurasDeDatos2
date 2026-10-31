using UnityEngine;

public enum CheeseType
{
    BigCheese = 0,
    NormalCheese = 1,
    AnormalCheese = 2,
    LittleCheese = 3
}

public class Cheese : MonoBehaviour
{
    public CheeseType type;
}
