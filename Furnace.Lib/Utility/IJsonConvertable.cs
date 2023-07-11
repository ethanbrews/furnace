namespace Furnace.Lib.Utility;

public interface IJsonConvertable<out T>
{
    public static abstract T FromJson(string jsonString);
}