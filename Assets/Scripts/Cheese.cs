using UnityEngine;

// Identifica cada queso. El número de cada uno (0..3) se usa
// también en los botones del menú.
public enum CheeseType
{
    BigCheese = 0,
    NormalCheese = 1,
    AnormalCheese = 2,
    LittleCheese = 3
}

// Se pone en cada GameObject de queso de la escena de juego.
public class Cheese : MonoBehaviour
{
    public CheeseType type;
}
