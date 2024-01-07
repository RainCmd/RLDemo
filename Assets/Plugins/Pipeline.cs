public class Pipeline<T>
{
    private readonly T[] values;
    private long start, end;
    public Pipeline(long size)
    {
        values = new T[size];
        start = end = 0;
    }
    public bool En(T value)
    {
        if (start < end - values.Length)
        {
            values[end++] = value;
            return true;
        }
        return false;
    }
    public bool De(out T value)
    {
        if (start < end)
        {
            value = values[start++];
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
