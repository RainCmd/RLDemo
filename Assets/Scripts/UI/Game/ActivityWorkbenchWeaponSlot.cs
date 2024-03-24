using UnityEngine;

public class ActivityWorkbenchWeaponSlot : MonoBehaviour
{
    private ActivityWorkbench workbench;
    private ActivityWorkbenchOperableNode node;
    public int wand;
    public int slot;
    public void Init(ActivityWorkbench workbench, int wand, int slot)
    {
        this.workbench = workbench;
        this.wand = wand;
        this.slot = slot;
        node = null;
    }
    public void SetNode(long nodeId)
    {
        if (nodeId != 0)
        {
            if (node == null)
            {
                node = workbench.GetNode(transform);
            }
            node.SetNode(nodeId);
        }
        else if (node != null)
        {
            workbench.RecyleNode(node);
            node = null;
        }
    }
}
