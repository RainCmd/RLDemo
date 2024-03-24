using RainLanguage;
using System;
using UnityEngine;

public static class Tool
{
    public static string Format(this string str, params object[] args)
    {
        return string.Format(str, args);
    }
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
    public static Vector2 ToVector(this Real2 vector)
    {
        return new Vector2((float)vector.x, (float)vector.y);
    }
    public static Vector3 ToVector(this Real3 vector)
    {
        return new Vector3((float)vector.x, (float)vector.y, (float)vector.z);
    }
}
