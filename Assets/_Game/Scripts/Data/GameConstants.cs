public static class GameConstants
{
    public static class Tags
    {
        public const string Player = "Player";
        public const string Death = "Death";
        public const string AttachPoint = "AttachPoint";
    }

    public static class AudioNames
    {
        public const string GameOver = "GameOver";
    }

    /// <summary>本地存档文件名，完整路径见 SaveManager.SaveFilePath。</summary>
    public const string SaveFileName = "save.json";

    /// <summary>关卡总数，与 LevelDatabase.asset 中 levels 数量一致。</summary>
    public const int LevelCount = 4;

    /// <summary>
    /// 场景名称常量，格式为 level + 序号。
    /// 须与 Build Settings 中场景文件名（不含 .unity）完全一致。
    /// </summary>
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string Level1 = "level1";
        public const string Level2 = "level2";
        public const string Level3 = "level3";
        public const string Level4 = "level4";
    }
}
