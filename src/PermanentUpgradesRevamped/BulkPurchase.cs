using HarmonyLib;
using Il2Cpp;
namespace PermanentUpgradesRevamped;
internal static class BulkPurchase
{
    internal static bool Running;
    internal static string LastResult="";
    static string LastPreview="";
    static Dictionary<int,SkillNode> Nodes()
    {
        var tree=GameManager.Instance?.skillTreeView;var nodes=new Dictionary<int,SkillNode>();
        if(tree==null||tree.nodes==null)return nodes;
        foreach(var node in tree.nodes)if(node!=null&&node.skill!=null)nodes.Add((int)node.skill.skillId,node);
        return nodes;
    }
    static BulkResult Plan(Dictionary<int,SkillNode> nodes,long budget)
    {
        var graph=new List<BulkNode>();
        foreach(var pair in nodes){var node=pair.Value;var requirements=new List<int>();foreach(var parent in node.requirements)if(parent!=null&&parent.skill!=null)requirements.Add((int)parent.skill.skillId);
            graph.Add(new BulkNode{Id=pair.Key,Price=node.skill.price,Purchased=node.skill.isPurchased,Excluded=node.IsDemoLocked||!node.gameObject.activeSelf||RobotRetirement.IsRetired(pair.Key)||SaveMigration.IsMoved(pair.Key),Requires=requirements.ToArray()});}
        return BulkPlan.Build(graph,budget);
    }
    internal static BulkResult Preview(){var budget=MoneyManager.Instance?.Money??0;var result=Plan(Nodes(),budget);var key=$"{budget}:{result.Ids.Length}:{result.Cost}";if(key!=LastPreview){LastPreview=key;Mod.Self.LoggerInstance.Msg($"Bulk preview: budget {budget} dollars, {result.Ids.Length} cheapest available purchases, total {result.Cost}, remainder {budget-result.Cost}.");}return result;}
    internal static void Buy()
    {
        if(Running||!NativeUi.CanBulkShop()||!Mod.CanShop())return;
        var nodes=Nodes();var money=MoneyManager.Instance;if(money==null)return;var plan=Plan(nodes,money.Money);
        if(money==null||plan.Ids.Length==0||money.Money<plan.Cost)return;
        var count=0;long spent=0;Running=true;
        try {
            foreach(var id in plan.Ids){
                var node=nodes[id];if(node.skill.isPurchased)continue;
                if(node.IsLocked||node.IsDemoLocked||money.Money<node.skill.price)break;
                var price=node.skill.price;node.OnClick();
                if(!node.skill.isPurchased)break;
                count++;spent+=price;
            }
            LastResult=$"{count} upgrades · {spent:N0} $";
            Mod.Self.LoggerInstance.Msg($"Bulk run purchases: {count}/{plan.Ids.Length}, total {spent} dollars, remaining {money.Money}.");
        }catch(Exception ex){LastResult=$"{count} upgrades purchased; see the log";Mod.Self.LoggerInstance.Error("Bulk purchase stopped: "+ex);}
        finally{Running=false;SaveManager.Instance.Save();if(SkillPopUp.Instance!=null)SkillPopUp.Instance.Hide();}
        if(count>0&&UISound.Instance!=null)UISound.Instance.PlayPurchaseClip();
    }
    [HarmonyPatch(typeof(UISound),"PlayPurchaseClip")]
    static class QuietBatchPatch{static bool Prefix()=>!Running;}
    [HarmonyPatch(typeof(SkillPopUp),"Show",new[]{typeof(SkillNode)})]
    static class QuietPopupPatch{static bool Prefix()=>!Running;}
}
