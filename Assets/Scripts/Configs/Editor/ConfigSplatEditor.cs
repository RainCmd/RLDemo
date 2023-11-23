using UnityEditor;

public class ConfigSplatEditor : ConfigEditor
{
    [MenuItem("配置文件/地板贴图")]
    private static void CreateConfigSplat()
    {
        Create<ConfigSplat>(ConfigSplat.path);
    }
}
