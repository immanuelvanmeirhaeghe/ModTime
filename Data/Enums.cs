namespace ModTime.Data.Enums
{
    /// <summary>
    /// Enumerates ModAPI supported game identifiers.
    /// </summary>
    public enum GameID
    {
        EscapeThePacific,
        GreenHell,
        SonsOfTheForest,
        Subnautica,
        TheForest,
        TheForestDedicatedServer,
        TheForestVR
    }

    public enum DayCycles
    {
        Daytime,
        Night
    }

    public enum TimeScaleModes
    {
        Normal = 0,
        Fast = 1,
        VeryFast = 2,       
        Paused = 3,
        Custom = 4
    }

    public enum TimeScaleFactor
    {
        Normal = 0,
        Medium = 1,
        High = 2,
        Paused = 3,
        Custom = 4
    }

    public enum MessageType
    {
        Info,
        Warning,
        Error
    }
}
