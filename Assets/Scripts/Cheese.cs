using UnityEngine;

// Identifica cada queso. El número de cada uno (0..3) se usa
// también en los botones del menú.
public enum CheeseType
{
    BigCheese = 0,
    NormalCheese = 1,
    AnormalCheese = 2,
    LittleCheese = 3,
    BonusCheese = 4     // quesos extra creados durante el juego (no tienen botón en el menú)
}

// Se pone en cada GameObject de queso de la escena de juego.
public class Cheese : MonoBehaviour
{
    public CheeseType type;

    // Nombre para mostrar en consola/pantalla. Los quesos extra comparten el mismo
    // 'type', así que usamos el nombre del GameObject (BonusCheese_1, _2, ...).
    public string Label => type == CheeseType.BonusCheese ? name : type.ToString();
}
