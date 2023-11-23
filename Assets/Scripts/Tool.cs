using System;
using UnityEngine;
using UnityEngine.Analytics;

public static class Tool
{
    public static Color GetDelayColor(int delay)
    {
        if (delay <= 0) return Color.clear;
        else if (delay < 600)
        {
            return new Color(Mathf.Sqrt(delay / 600f), 1, 0);
        }
        else
        {
            var v = (delay - 600) * .01f;
            return new Color(1, 1 / (v * v + 1), 0);
        }
    }
    public static int GetDelay(long ticks)
    {
        return (int)new TimeSpan(Math.Max(DateTime.Now.Ticks - ticks, 0)).TotalMilliseconds;
    }
}
