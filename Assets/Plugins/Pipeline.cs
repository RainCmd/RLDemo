public class Pipeline<T>
{
    private readonly T[] values;
    private long start, end, mask;
    public Pipeline(int level)
    {
        values = new T[1 << level];
        start = end = 0;
        mask = values.Length - 1;
    }
    public bool En(T value)
    {
        if (start < end - values.Length)
        {
            values[(end++) & mask] = value;
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
