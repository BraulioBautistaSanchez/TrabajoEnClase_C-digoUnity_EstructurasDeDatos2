using System.Collections.Generic;

public enum RouteMode
{
    Normal, Reversed, Custom      
}

public static class GameSettings
{
    public static RouteMode Mode = RouteMode.Normal;
    public static List<CheeseType> CustomOrder = new List<CheeseType>();
}
