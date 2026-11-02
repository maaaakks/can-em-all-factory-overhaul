using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace PermanentUpgradesRevamped;

internal static class FactoryExtension
{
    static bool Retired(int id)=>RobotRetirement.IsRetired(id)||SaveMigration.IsMoved(id);
    static SkillTreeView Tree;
    static bool Building, Failed, PoolsReady;
    static float NextRefresh;
    static readonly List<SkillNode> NewNodes=new();
    static readonly List<Turret> ExtraTurrets=new();
    static readonly Dictionary<int,Turret> NativeTurrets=new();
    static string LastFleet="";
    static float AppliedAmmo;
    static IntPtr LastManager;
    static HashSet<int> Owned=new();

    internal static void Ensure(SkillTreeView tree)
    {
        if(tree==null||Building||Failed||Tree==tree)return;
        Building=true;
        try {
            var original=tree.nodes?.ToArray();
            if(original==null||original.Length==0)original=tree.GetComponentsInChildren<SkillNode>(true).ToArray();
            if(original.Length==0)return;
            var byId=original.Where(n=>n!=null&&n.skill!=null).GroupBy(n=>(int)n.skill.skillId).ToDictionary(g=>g.Key,g=>g.First());
            foreach(var node in original) if(Retired((int)node.skill.skillId))node.gameObject.SetActive(false);
            var kept=original.Where(n=>!Retired((int)n.skill.skillId)).ToArray();
            void Expand(SkillNode requirement,HashSet<IntPtr> visited,List<SkillNode> target) {
                if(requirement==null||!visited.Add(requirement.Pointer))return;
                if(!Retired((int)requirement.skill.skillId)){target.Add(requirement);return;}
                foreach(var parent in requirement.requirements)Expand(parent,visited,target);
            }
            foreach(var node in kept) {
                var requirements=new List<SkillNode>();var visited=new HashSet<IntPtr>();
                foreach(var requirement in node.requirements)Expand(requirement,visited,requirements);
                node.requirements=new Il2CppSystem.Collections.Generic.List<SkillNode>();foreach(var requirement in requirements)node.requirements.Add(requirement);
                node.RebuildLines();
            }
            original=kept;
            var carrier=original.First(n=>n.skill.TryCast<MoveSpeedSkill>()!=null);
            Vector2 Position(SkillNode n){var p=tree.content.InverseTransformPoint(n.transform.position);return new Vector2(p.x,p.y);}
            var occupied=original.Select(n=>Position(n)).ToList();

            var step=Math.Max(80,carrier.GetComponent<RectTransform>().rect.width*1.8f);
            var mask=tree.viewport.GetComponent<RectMask2D>() ?? tree.viewport.gameObject.AddComponent<RectMask2D>();
            foreach(var def in FactoryPlan.All) {
                var iconId=def.Kind=="turrets"?264:def.Kind=="ammo"?270:def.Kind.StartsWith("suction")?214:155;
                var node=Object.Instantiate(carrier,tree.content);
                node.name="PUR_"+def.Name;
                node.skill=node.GetComponent<MoveSpeedSkill>() ?? node.gameObject.AddComponent<MoveSpeedSkill>();
                if(node.skill.Pointer==carrier.skill.Pointer)throw new InvalidOperationException("Shared skill instance in cloned node");
                node.skill.skillId=(SkillId)def.Id;node.skill.price=def.Price;node.skill.isPurchased=false;
                node.lockedForDemo=false;node.skillTreeView=tree;
                node.requirements=new Il2CppSystem.Collections.Generic.List<SkillNode>();node.requirements.Add(byId[def.Parent]);
                foreach(var line in node.lines)if(line!=null&&line.transform.IsChildOf(node.transform))Object.Destroy(line.gameObject);
                node.lines=new Il2CppSystem.Collections.Generic.List<Image>();
                if(!node.lineContainer.IsChildOf(node.transform)) {
                    var lineRoot=new GameObject("PUR_Lines",new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppSystem.Type>(new[]{Il2CppInterop.Runtime.Il2CppType.Of<RectTransform>()}));
                    lineRoot.transform.SetParent(node.transform,false);node.lineContainer=lineRoot.GetComponent<RectTransform>();
                }
                node.icon.sprite=byId[iconId].icon.sprite;
                var style=byId[def.Kind.StartsWith("suction")?154:iconId];node.purchasedColor=style.purchasedColor;node.purchasedIconColor=style.purchasedIconColor;
                var rt=node.GetComponent<RectTransform>();
                var parent=byId[def.Parent];var parentPos=Position(parent);
                var direction=parent.requirements.Count>0?parentPos-Position(parent.requirements[0]):parentPos-Position(tree.rootNode);
                if(direction.sqrMagnitude<1)direction=Vector2.right;direction=direction.normalized;
                var chosen=parentPos+direction*step;var found=false;
                foreach(var distance in new[]{1f,1.4f,1.8f,2.2f}) { foreach(var degrees in new[]{0,30,-30,60,-60,90,-90,120,-120,150,-150,180}) {
                    var rotation=Quaternion.Euler(0,0,degrees);var rotated=rotation*new Vector3(direction.x,direction.y,0);
                    var candidate=parentPos+new Vector2(rotated.x,rotated.y)*step*distance;
                    if(occupied.All(p=>(p-candidate).sqrMagnitude>step*step*.65f)){chosen=candidate;found=true;break;}
                } if(found)break; }
                if(!found)throw new InvalidOperationException("No non-overlapping placement for "+def.Name);
                rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.localPosition=new Vector3(chosen.x,chosen.y,0);rt.localScale=Vector3.one;occupied.Add(chosen);
                node.gameObject.SetActive(true);node.canvasGroup.alpha=1;
                byId.Add(def.Id,node);NewNodes.Add(node);
            }
            tree.nodes=new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<SkillNode>(original.Concat(NewNodes).ToArray());
            if(NewNodes.Select(n=>n.skill.Pointer).Distinct().Count()!=FactoryPlan.All.Length || NewNodes.Select(n=>(int)n.skill.skillId).Distinct().Count()!=FactoryPlan.All.Length)
                throw new InvalidOperationException("Factory skill identities are not unique");
            if(SkillManager.Instance!=null)SkillManager.Instance.skillNodes=tree.nodes;
            var requiredWidth=(occupied.Max(p=>Math.Abs(p.x))+step)*2;
            var size=tree.content.sizeDelta;size.x=Math.Max(size.x,requiredWidth);size.y=Math.Max(size.y,(occupied.Max(p=>Math.Abs(p.y))+step)*2);tree.content.sizeDelta=size;
            foreach(var node in NewNodes){node.RebuildLines();node.ApplyInitialState();node.RefreshLockState();}
            Tree=tree;
            var probe=new SaveData();probe.unlockedSkills.Add((SkillId)FactoryPlan.All[0].Id);
            var restored=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(probe));
            if((int)restored.unlockedSkills[0]!=FactoryPlan.All[0].Id)throw new InvalidOperationException("Native JSON cannot preserve custom skill IDs");
            Mod.Self.LoggerInstance.Msg($"Factory extension: injected {NewNodes.Count} native turret/ammo nodes; grinder upgrades are permanent. Branches extended from native endpoints; viewport clipping enabled.");
            Mod.Self.LoggerInstance.Msg("Factory audit: unique skill objects and IDs; native SaveData JSON roundtrip passed; retired robot nodes hidden and dependencies bypassed.");
        } catch(Exception ex){Failed=true;foreach(var node in NewNodes)if(node!=null)node.gameObject.SetActive(false);Mod.Self.LoggerInstance.Error("Factory tree injection failed: "+ex);}
        finally{Building=false;}
    }

    static void EnsurePools(GameManager gm)
    {
        if(PoolsReady)return;
        var turrets=gm.turrets.ToArray();
        if(turrets.Length!=2)throw new InvalidOperationException("Unexpected native machine counts; refusing duplicate pools");
        foreach(var node in Tree.nodes) {
            var unlock=node.skill.TryCast<UnlockTurretSkill>();
            if(unlock!=null)NativeTurrets[(int)unlock.skillId]=unlock.turret;
        }
        if(NativeTurrets.Count!=2)throw new InvalidOperationException("Native turret unlock references missing");
        var minT=turrets.Min(t=>t.transform.position.z);var maxT=turrets.Max(t=>t.transform.position.z);
        var gap=maxT-minT;
        for(var i=0;i<2;i++) {
            var turret=Object.Instantiate(turrets[i],turrets[i].transform.parent);
            turret.name="PUR_Turret_"+(i+3);turret.gameObject.SetActive(false);
            var p=turret.transform.position;p.z=i==0?minT-gap:maxT+gap;turret.transform.position=p;
            turret.TurnOff();ExtraTurrets.Add(turret);
        }
        foreach(var robot in Object.FindObjectsOfType<RobotCleaner>(true)) {
            robot.StopRobot();robot.gameObject.SetActive(false);
        }
        gm.robotCleaners=new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<RobotCleaner>(0);
        gm.turrets=new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Turret>(turrets.Concat(ExtraTurrets).ToArray());
        PoolsReady=true;
        Mod.Self.LoggerInstance.Msg("Factory pools: 4 turrets registered, all robots disabled; robot manager array is empty.");
    }

    internal static void Tick()
    {
        if(Failed||Time.unscaledTime<NextRefresh)return;NextRefresh=Time.unscaledTime+.5f;
        try {
            var gm=GameManager.Instance;
            if(gm==null||SaveManager.Instance?.CurrentSaveData==null)return;
            if(Tree==null)Ensure(gm.skillTreeView);
            if(Tree==null)return;
            EnsurePools(gm);Reconcile();
        }catch(Exception ex){Failed=true;Mod.Self.LoggerInstance.Error("Factory extension stopped: "+ex);}
    }

    static void Reconcile()
    {
        var gm=GameManager.Instance;
        if(gm==null||!PoolsReady)return;
        var save=SaveManager.Instance?.CurrentSaveData;if(save==null)return;
        Owned=new HashSet<int>();foreach(var id in save.unlockedSkills)Owned.Add((int)id);
        if(LastManager!=gm.Pointer){AppliedAmmo=0;LastManager=gm.Pointer;}
        var ammo=(float)FactoryPlan.Target("ammo",Owned,0);
        gm.turretAmmoCapacityMultiplier+=ammo-AppliedAmmo;
        AppliedAmmo=ammo;
        var count=(int)FactoryPlan.Target("turrets",Owned,2);
        foreach(var pair in NativeTurrets) {
            var enabled=Owned.Contains(pair.Key);var turret=pair.Value;
            if(turret.gameObject.activeSelf!=enabled){turret.gameObject.SetActive(enabled);if(enabled)turret.RestoreAmmo();}
        }
        for(var i=0;i<ExtraTurrets.Count;i++) {
            var turret=ExtraTurrets[i];var enabled=i+3<=count;
            if(turret.gameObject.activeSelf!=enabled){turret.gameObject.SetActive(enabled);if(enabled)turret.RestoreAmmo();}
        }
        var fleet=string.Join("; ",gm.turrets.Select(t=>$"{t.name}: active={t.gameObject.activeInHierarchy}, z={t.transform.position.z:0.00}"));
        if(fleet!=LastFleet){LastFleet=fleet;Mod.Self.LoggerInstance.Msg("Turret coverage: "+fleet);}
        IntakeGeometry.Ensure(gm);IntakeGeometry.Apply((float)Balance.Value(8,Mod.Ranks[8]));
        IndustrialSuction.Configure();
    }

    static bool ApplyCustom(Skill skill)
    {
        var def=FactoryPlan.Find((int)skill.skillId);if(def==null)return true;
        // ApplyInitialState runs before the finished tree and native unlock references are available.
        if(Building||Tree==null)return false;
        if(!PoolsReady)EnsurePools(GameManager.Instance);
        Reconcile();
        if(!Building&&Owned.Contains(def.Id))Mod.Self.LoggerInstance.Msg("Factory upgrade applied: "+def.Name);
        return false;
    }

    [HarmonyPatch(typeof(SkillTreeView),"Awake")]
    static class AwakePatch{static void Postfix(SkillTreeView __instance)=>Ensure(__instance);}
    [HarmonyPatch(typeof(SkillTreeView),"ApplySave")]
    static class LoadPatch{static void Prefix(SkillTreeView __instance)=>Ensure(__instance);}
    [HarmonyPatch(typeof(SkillManager),"Awake")]
    static class ManagerPatch{static void Postfix(SkillManager __instance){if(Tree!=null)__instance.skillNodes=Tree.nodes;}}
    [HarmonyPatch(typeof(GameManager),"ResetToDefaultValues")]
    static class ResetPatch{static void Postfix(){AppliedAmmo=0;}}
    [HarmonyPatch(typeof(MoveSpeedSkill),"ApplySkill")]
    static class SkillPatch{static bool Prefix(MoveSpeedSkill __instance)=>ApplyCustom(__instance);}
    [HarmonyPatch(typeof(MoveSpeedSkill),"GetFromValueText")]
    static class FromPatch{static bool Prefix(MoveSpeedSkill __instance,ref string __result){var d=FactoryPlan.Find((int)__instance.skillId);if(d==null)return true;__result="";return false;}}
    [HarmonyPatch(typeof(MoveSpeedSkill),"GetToValueText")]
    static class ToPatch{static bool Prefix(MoveSpeedSkill __instance,ref string __result){var d=FactoryPlan.Find((int)__instance.skillId);if(d==null)return true;__result=d.Name;return false;}}
    [HarmonyPatch(typeof(SkillPopUp),"Show",new[]{typeof(SkillNode)})]
    static class PopupPatch{static void Postfix(SkillPopUp __instance,SkillNode node){var d=FactoryPlan.Find((int)node.skill.skillId);if(d==null)return;__instance.titleText.text=d.Name;__instance.descriptionText.text=d.Description;__instance.stats.SetActive(false);}}
    [HarmonyPatch(typeof(UnlockRobotSkill),"ApplySkill")]
    static class RetiredUnlockPatch{static bool Prefix()=>false;}
    [HarmonyPatch(typeof(RobotCleaner),"StartRobot")]
    static class RetiredStartPatch{static bool Prefix(RobotCleaner __instance){__instance.gameObject.SetActive(false);return false;}}
    [HarmonyPatch(typeof(LanguageManager),"Text")]
    static class TextPatch{static bool Prefix(string id,ref string __result){if(id==null)return true;var digits=new string(id.TakeWhile(char.IsDigit).ToArray());if(!int.TryParse(digits,out var key))return true;var d=FactoryPlan.Find(key);if(d==null)return true;__result=digits==id?d.Name:d.Description;return false;}}
}
