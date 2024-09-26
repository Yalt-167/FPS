#define DEV_BUILD

namespace SceneHandling
{
    public static class Scenes
    {
        public static string GetSceneFromGamemodeAndMap(string gamemode, string map)
        {
            return $"_Scenes/{gamemode}/{map}Data/{map}";
        }

        public static readonly string LoginScene = $"_Scenes/{nameof(LoginScene)}";


        public static class HUD
        {
    #if DEV_BUILD
            public static readonly string DebugOverlay = $"_Scenes/HUD/{nameof(DebugOverlay)}";
    #endif

            public static readonly string MainScoreboardHUD = $"_Scenes/HUD/{nameof(MainScoreboardHUD)}";
        }

    }
}