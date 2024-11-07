namespace MyUtility
{
    public static class AssetLoader
    {
        public static T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = UnityEngine.Resources.Load<T>(path);
            if (asset == null)
            {
                UnityEngine.Debug.LogError($"No proper asset found at {path}");
            }

            return asset;
        }
    }
}