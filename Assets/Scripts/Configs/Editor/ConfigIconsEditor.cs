using UnityEditor;

public class ConfigIconsEditor : ConfigEditor
{
    [MenuItem("配置文件/玩家头像")]
    private static void CreateHeadIcons()
    {
        Create<ConfigIcons>(ConfigIcons.HeadIconsPath);
    }
    [MenuItem("配置文件/法术节点类型图标")]
    private static void CreateMagicNodeTypeIcons()
    {
        Create<ConfigIcons>(ConfigIcons.MagicNodeTypeIconsPath);
    }
}
