using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.UI;

public class ActivityWorkbenchNodeDetailPanel : MonoBehaviour
{
    public Image icon;
    public Text nodeName;
    public RectTransform propertyContent;
    public GameObject propertyPrefab;
    public Image type;
    public Text description;
    public void Show(LogicMagicNodeEntity entity)
    {
        if (LogicConfig.magicNodes.TryGet(item => item.id == entity.configId, out var cfg))
        {
            gameObject.SetActive(true);
            icon.sprite = Config.NodeIconList[(int)cfg.icon];
            type.sprite = Config.MagicNodeTypeIcons[(int)cfg.type];
            nodeName.text = cfg.name;
            description.text = cfg.desc;
        }
    }
}
