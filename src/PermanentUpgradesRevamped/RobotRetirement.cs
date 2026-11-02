using Newtonsoft.Json.Linq;

namespace PermanentUpgradesRevamped;

public static class RobotRetirement
{
    public static readonly Dictionary<int,int> Prices=new() {
        [254]=25000,[255]=100000,[256]=250000,[257]=500000,[258]=750000,[259]=1000000,
        [260]=50000,[261]=100000,[262]=500000,[263]=1000000,
        [10021]=3000000,[10022]=10000000,[10023]=30000000,[10024]=100000000,
        [10031]=2000000,[10032]=5000000,[10033]=15000000,[10034]=40000000,
        [10041]=2000000,[10042]=10000000,[10043]=40000000
    };
    public static bool IsRetired(int id)=>Prices.ContainsKey(id);
    public static JObject Upgrade(JObject original,out long refund)
    {
        var root=(JObject)original.DeepClone();refund=0;
        if(root["unlockedSkills"] is not JArray skills)return root;
        var ids=skills.ToObject<int[]>();
        refund=ids.Distinct().Where(IsRetired).Sum(id=>(long)Prices[id]);
        if(refund==0)return root;
        root["money"]=checked(((long?)root["money"]??0)+refund);
        root["unlockedSkills"]=JArray.FromObject(ids.Where(id=>!IsRetired(id)));
        return root;
    }
}
