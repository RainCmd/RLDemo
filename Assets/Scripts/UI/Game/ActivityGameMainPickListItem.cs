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
    private long nodeId;
    private GameMgr mgr;
    public void Init(GameMgr mgr, long nodeId)
    {
        this.mgr = mgr;
        this.nodeId = nodeId;
        foreach (var node in LogicConfig.magicNodes)
            if (nodeId == node.id)
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
        mgr.Room.UpdateOperator(Operator.Pick(nodeId));
    }
}
