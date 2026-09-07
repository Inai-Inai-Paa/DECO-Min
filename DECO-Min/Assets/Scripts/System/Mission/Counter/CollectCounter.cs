using System.Collections.Generic;

public class CollectCounter
{
    private Dictionary<CollectData, int> Collect = new();
    public int TotalCollectCount { get; private set; }

    public void AddCollectCount(CollectData collect)
    {
        TotalCollectCount++;

        if (!Collect.ContainsKey(collect))
        {
            Collect[collect] = 0;
        }

        Collect[collect]++;
    }

    public int GetCollectCount(CollectData collect)
    {
        if (Collect.TryGetValue(collect, out int count))
        {
            return count;
        }
        return 0;
    }
}
