using Newtonsoft.Json.Linq;
namespace PermanentUpgradesRevamped;
public static class SaveMigration
{
    static readonly int[][] OldPrices={new[]{2,3,7,15,10,15,25,40,65,100,150,225},new[]{5,4,6,8,12,15,20,30,50,80,125,190},new[]{5,8,12,16,24,30,40,55,85,130,195,290},new[]{10,10,15,25,40,65,100,150,225},new[]{5,10,15,25,40,65},new[]{5,10,15,25,40,65}};
    static readonly int[] SpecialPrices={25,20,20,25,15,30,30,10};
    public static readonly Dictionary<int,int> MovedPrices=new(){[10051]=250000,[10052]=5000000,[10053]=50000000,[10101]=25000,[10111]=100000,[10112]=500000,[10113]=2000000,[10114]=10000000,[10121]=50000,[10122]=250000,[10123]=1000000,[10124]=5000000};
    public static bool IsMoved(int id)=>MovedPrices.ContainsKey(id);
    public static JObject Upgrade(JObject original,out int refund)=>Upgrade(original,out refund,out _);
    public static JObject Upgrade(JObject original,out int refund,out long cashRefund)
    {
        refund=0;cashRefund=0;var root=(JObject)original.DeepClone();
        if(root["pur"] is JObject state) {
            var schema=(int?)state["schema"];
            if(schema==6){Balance.Validate(state["ranks"].ToObject<int[]>());return root;}
            if(schema==5){UpgradeFirstDelivery(root);return root;}
            if(schema==4){refund=UpgradeLoadout(root);UpgradeFirstDelivery(root);return root;}
            if(schema is not (1 or 2 or 3))throw new InvalidDataException("Unknown PUR save schema");
            var ranks=state["ranks"]?.ToObject<int[]>();
            if(ranks==null||ranks.Length!=6)throw new InvalidDataException("Invalid old rank count");
            if(schema==3) {
                for(var i=0;i<6;i++){if(ranks[i]<0||ranks[i]>OldPrices[i].Length)throw new InvalidDataException("Invalid legacy rank");refund=checked(refund+OldPrices[i].Take(ranks[i]).Sum());}
            }else {
                var caps=new[]{20,30,30,20,15,10};var prices=new[]{25,30,40,35,25,20};
                for(var i=0;i<6;i++){if(ranks[i]<0||ranks[i]>caps[i])throw new InvalidDataException("Invalid legacy rank");for(var r=0;r<ranks[i];r++)refund=checked(refund+(int)Math.Ceiling(prices[i]*Math.Pow(1.35,r)));}
            }
            var owned=state["specials"]?.ToObject<bool[]>()??new bool[8];if(owned.Length!=8)throw new InvalidDataException("Invalid legacy specials");
            for(var i=0;i<8;i++)if(owned[i])refund=checked(refund+SpecialPrices[i]);
            root["upgradePoints"]=checked(((int?)root["upgradePoints"]??0)+refund);
            state["schema"]=4;state["ranks"]=JArray.FromObject(new int[18]);state.Remove("specials");state["rebalanceRefund"]=refund;
        }
        if(root["unlockedSkills"] is JArray skills) {
            var ids=skills.ToObject<int[]>();cashRefund=ids.Distinct().Where(IsMoved).Sum(id=>(long)MovedPrices[id]);
            root["unlockedSkills"]=JArray.FromObject(ids.Where(id=>!IsMoved(id)));
            if(cashRefund>0)root["money"]=checked(((long?)root["money"]??0)+cashRefund);
        }
        refund=checked(refund+UpgradeLoadout(root));UpgradeFirstDelivery(root);
        return root;
    }
    static int UpgradeLoadout(JObject root)
    {
        if(root["pur"] is not JObject state||(int?)state["schema"]!=4)return 0;
        var old=state["ranks"]?.ToObject<int[]>();if(old==null||old.Length!=18)throw new InvalidDataException("Invalid old permanent ranks");
        var next=new int[19];Array.Copy(old,next,18);
        var endurance=old[12];if(endurance<0||endurance>10)throw new InvalidDataException("Invalid endurance rank");
        next[12]=0;var padded=new int[Balance.Upgrades.Length];Array.Copy(next,padded,19);Balance.Validate(padded);var refund=10*endurance*(endurance+1)/2;
        root["upgradePoints"]=checked(((int?)root["upgradePoints"]??0)+refund);
        state["schema"]=5;state["ranks"]=JArray.FromObject(next);state["enduranceRefund"]=refund;
        return refund;
    }
    static void UpgradeFirstDelivery(JObject root)
    {
        if(root["pur"] is not JObject state||(int?)state["schema"]!=5)return;
        var old=state["ranks"]?.ToObject<int[]>();if(old==null||old.Length!=19)throw new InvalidDataException("Invalid schema 5 permanent ranks");
        var next=new int[Balance.Upgrades.Length];Array.Copy(old,next,19);Balance.Validate(next);
        state["schema"]=6;state["ranks"]=JArray.FromObject(next);
    }

}
