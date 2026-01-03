using Storia.Constants;

public sealed class DeterministicRng
{
    private uint _state;
    private int _callCount = 0;

    public DeterministicRng(int seed)
    {
        // Seed 0 olmasın diye karıştır
        uint s = unchecked((uint)seed);
        if (s == 0u) s = GameConstants.DefaultRngSeed;
        _state = s;
    }

    private uint NextU32()
    {
        _callCount++; // Meta veri izleme - durumu veya çıktıları etkilemez
        uint x = _state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _state = x;
        return x;
    }

    /// <summary>
    /// Oluşturulmadan bu yana yapılan toplam RNG çağrısı sayısını döner.
    /// Determinizm doğrulaması için kullanılır - RNG davranışını etkilemez.
    /// </summary>
    public int GetCallCount() => _callCount;

    public float Next01()
    {
        // [0,1)
        uint v = NextU32();
        // 24-bit mantissa hassasiyeti yeter
        return (v & GameConstants.MantissaMask) / GameConstants.MantissaPrecision;
    }

    public int RangeInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            return minInclusive;

        uint span = (uint)(maxExclusive - minInclusive);
        uint v = NextU32();
        int result = (int)(v % span) + minInclusive;
        return result;
    }

    public bool Chance(float probability01)
    {
        if (probability01 <= 0f) return false;
        if (probability01 >= 1f) return true;
        return Next01() < probability01;
    }

    /// <summary>
    /// Listeden rastgele bir eleman seç.
    /// </summary>
    public T NextFromList<T>(System.Collections.Generic.List<T> list)
    {
        if (list == null || list.Count == 0)
            throw new System.ArgumentException("List cannot be null or empty");

        int index = RangeInt(0, list.Count);
        return list[index];
    }

    /// <summary>
    /// Diziyi Fisher-Yates algoritması ile karıştır (in-place).
    /// </summary>
    public void Shuffle<T>(T[] array)
    {
        if (array == null || array.Length <= 1)
            return;

        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = RangeInt(0, i + 1);
            T temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }

    /// <summary>
    /// Listeyi Fisher-Yates algoritması ile karıştır (in-place).
    /// </summary>
    public void Shuffle<T>(System.Collections.Generic.List<T> list)
    {
        if (list == null || list.Count <= 1)
            return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = RangeInt(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
