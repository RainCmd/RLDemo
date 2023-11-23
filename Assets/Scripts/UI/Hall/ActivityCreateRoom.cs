using System;
using UnityEngine;
using UnityEngine.UI;

public class ActivityCreateRoom : UIActivity
{
    public InputField input;
    public void OnOKClick()
    {
        var roomName = input.text;
        try
        {
            var server = new RoomServer(roomName);
            UIManager.CloseAll();
            Show<ActivityRoom>("Room").Init(server);
        }
        catch (Exception e)
        {
            GameLog.Show(Color.red, e.Message);
        }
    }
}
