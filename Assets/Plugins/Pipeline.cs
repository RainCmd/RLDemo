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
        if (end - start > (uint)values.Length)
        {
            if (queue != null)
            {
                while (queue.Count > 0 && end - start > (uint)values.Length)
                    values[(end++) & mask] = queue.Dequeue();
                if(end - start <= (uint)values.Length)
                {
                    queue.Enqueue(value);
                    return true;
                }
            }
            values[(end++) & mask] = value;
            return true;
        }
        else if (queue != null)
        {
            queue.Enqueue(value);
            return true;
        }
        return false;
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
