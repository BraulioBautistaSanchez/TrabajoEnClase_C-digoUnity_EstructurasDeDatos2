using System.Collections.Generic;

public enum RouteMode
{
    Normal,     // Little -> ... -> Big (orden del programador)
    Reversed,   // Big -> ... -> Little
    Custom      // orden elegido por el jugador
}

// Clase estática: sobrevive al cambiar de escena, así el menú
// le "pasa" la decisión del jugador a la escena de juego.
public static class GameSettings
{
    public static RouteMode Mode = RouteMode.Normal;
    public static List<CheeseType> CustomOrder = new List<CheeseType>();
}
