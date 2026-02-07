// GameSettings.cs
public static class GameSettings
{
    // Map
    public static string mapScene = "";
    public static string mapName = "";
    public static string mapSize = "";

    // Gamemode
    public static int gamemode = 0; // 0=Collect, 1=Survival, 2=Swarm, 3=SurvivalAlt, 4=Sandbox

    // Settings
    public static int custards = 15;    // Collect / Swarm
    public static int timeLimit = 15;   // Collect / Swarm (minutes)
    public static int difficulty = 1;   // 0 Easy, 1 Normal, 2 Hard, 3 Insane
}
