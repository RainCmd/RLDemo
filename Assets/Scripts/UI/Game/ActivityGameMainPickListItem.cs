using UnityEngine;
using UnityEngine.UI;

public class ActivityGameMainPickListItem : MonoBehaviour
{
    [SerializeField]
    private Image icon;
    [SerializeField]
    private Image type;
    [SerializeField]
    private Text num;
    [SerializeField]
    private Text nameText;
    private LogicMagicNodeEntity entity;
    private GameMgr mgr;
    public void Init(GameMgr mgr, LogicMagicNodeEntity entity)
    {
        this.mgr = mgr;
        this.entity = entity;
        foreach (var node in LogicConfig.magicNodes)
            if (entity.configId == node.id)
            {
                icon.sprite = Config.NodeIconList[(int)node.icon];
                type.sprite = Config.MagicNodeTypeIcons[(int)node.type];
                num.text = node.number.ToString();
                num.gameObject.SetActive(node.number > 0);
                nameText.text = node.name;
                return;
            }
        gameObject.SetActive(false);
    }
    public void OnClick()
    {
        mgr.Room.UpdateOperator(Operator.Pick(entity.id));
    }
}
