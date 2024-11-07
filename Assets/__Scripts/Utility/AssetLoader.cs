namespace MyUtility
{
    public static class AssetLoader
    {
        public static T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            return UnityEngine.Resources.Load<T>(path) ?? throw new System.Exception($"No proper asset found at {path}");
        }
    }
}