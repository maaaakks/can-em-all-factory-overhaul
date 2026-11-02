namespace PermanentUpgradesRevamped;
public sealed class BulkNode
{
    public int Id,Price;public int[] Requires=Array.Empty<int>();public bool Purchased,Excluded;
}
public sealed class BulkResult {public int[] Ids;public long Cost;}
public static class BulkPlan
{
    public static BulkResult Build(IEnumerable<BulkNode> source,long budget)
    {
        if(budget<0)throw new ArgumentOutOfRangeException(nameof(budget));
        var nodes=source.ToArray();if(nodes.Select(n=>n.Id).Distinct().Count()!=nodes.Length)throw new InvalidDataException("Duplicate run skill IDs");
        var owned=nodes.Where(n=>n.Purchased).Select(n=>n.Id).ToHashSet();
        var candidates=nodes.Where(n=>!n.Purchased&&!n.Excluded&&n.Price>=0).OrderBy(n=>n.Price).ThenBy(n=>n.Id).ToArray();
        var result=new List<int>();var remaining=budget;
        while(true) {
            // Rescan after each purchase: an unlocked cheap child takes priority over the next costly node.
            var node=candidates.FirstOrDefault(n=>!owned.Contains(n.Id)&&n.Price<=remaining&&n.Requires.All(owned.Contains));
            if(node==null)break;
            owned.Add(node.Id);result.Add(node.Id);remaining-=node.Price;
        }
        return new BulkResult{Ids=result.ToArray(),Cost=budget-remaining};
    }
}
