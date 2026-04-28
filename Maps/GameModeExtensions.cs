public static class GameModeExtensions
{
    public static string ToDisplayString(this GameMode mode)
    {
        return mode switch
        {
            GameMode.OneVOne => "1v1",
            GameMode.FFA => "FFA",
            _ => mode.ToString()
        };
    }

    public static GameMode? FromString(string modeString)
    {
        if (string.IsNullOrEmpty(modeString))
            return null;

        return modeString.ToLower() switch
        {
            "1v1" or "onevone" => GameMode.OneVOne,
            "ffa" => GameMode.FFA,
            _ => null
        };
    }
}
