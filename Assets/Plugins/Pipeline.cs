using System.Collections.Generic;
public class Pipeline<T>
{
    private readonly T[] values;
    private uint start, end, mask;
    private readonly Queue<T> queue;
    public Pipeline(int level, bool cache = false)
    {
        values = new T[1 << level];
        start = end = 0;
        mask = (uint)values.Length - 1;
        queue = cache ? new Queue<T>() : null;
    }
    public bool En(T value)
    {
        if (end - start >= (uint)values.Length)
        {
            if (queue != null) queue.Enqueue(value);
            else return false;
        }
        else if (queue != null)
        {
            queue.Enqueue(value);
            while (queue.Count > 0)
                if (end - start >= (uint)values.Length) break;
                else values[(end++) & mask] = queue.Dequeue();
        }
        else values[(end++) & mask] = value;
        return true;
    }
    public bool De(out T value)
    {
        if (start < end)
        {
            value = values[(start++) & mask];
            return true;
        }
        value = default;
        return false;
    }
    public void Clear()
    {
        start = end = 0;
    }
}
