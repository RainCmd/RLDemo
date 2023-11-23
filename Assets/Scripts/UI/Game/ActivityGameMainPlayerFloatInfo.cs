using UnityEngine.UI;

public class ActivityGameMainPlayerFloatInfo : ActivityGameMainNpcFloatInfo
{
    public Image icon;
    public override void Init(GameMgr manager, GameUnit unit)
    {
        base.Init(manager, unit);
        OnOwnerChanged();
        unit.entity.OnOwnerChanged += OnOwnerChanged;
    }
    public override void Deinit()
    {
        Unit.entity.OnOwnerChanged -= OnOwnerChanged;
        base.Deinit();
    }
    private void OnOwnerChanged()
    {
        if (Manager.TryGetPlayer(Unit.entity.owner, out var info))
        {
            icon.gameObject.SetActive(true);
            icon.sprite = Config.PlayerHeadIconList[info.headIcon];
        }
        else icon.gameObject.SetActive(false);
    }
}
