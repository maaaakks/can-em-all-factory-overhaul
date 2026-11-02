using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

[assembly: MelonInfo(typeof(PermanentUpgradesRevamped.Mod), "Permanent Upgrades Revamped", "0.12.0", "Max & Codex")]
[assembly: MelonGame("LuisDev", "Can 'Em All")]
[assembly: HarmonyDontPatchAll]

namespace PermanentUpgradesRevamped;

public sealed class Mod : MelonMod
{
    internal static Mod Self = null!;
    internal static int[] Ranks = new int[Balance.Upgrades.Length];
    internal static bool Migrated, Loaded, Faulted;
    internal static string SaveFile = "";
    static string Notice = "Purchased bonuses apply immediately. Starting cash: next factory reset.";
    static float NextScan;
    static GameManager BonusOwner;

    static readonly Dictionary<IntPtr, float> GameBonuses = new(),AmmoBonuses=new();
    static readonly Dictionary<IntPtr, float> IncomeBonuses = new(), MoveBonuses = new();
    static readonly Dictionary<IntPtr, int> StorageBonuses = new(), BaseStorage = new();

    public override void OnInitializeMelon()
    {
        Self = this;
        // Isolate vanilla progress. Native HasSave/Load/Save/DeleteSave all use the patched SavePath.
        var saveDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "LuisDev", "Can 'Em All");
        SaveFile = Path.Combine(saveDir, "save.pur-v1.sav");
        if (!File.Exists(SaveFile) && File.Exists(Path.Combine(saveDir, "save.sav")))
            File.Copy(Path.Combine(saveDir, "save.sav"), SaveFile);
        HarmonyInstance.PatchAll(typeof(Mod).Assembly);
        if(HarmonyLib.Harmony.GetAllPatchedMethods().Any(m=>HarmonyLib.Harmony.GetPatchInfo(m).Prefixes.Where(p=>p.owner==HarmonyInstance.Id).GroupBy(p=>p.PatchMethod).Any(g=>g.Count()>1) || HarmonyLib.Harmony.GetPatchInfo(m).Postfixes.Where(p=>p.owner==HarmonyInstance.Id).GroupBy(p=>p.PatchMethod).Any(g=>g.Count()>1)))
            throw new InvalidOperationException("Duplicate Harmony registration detected");
        LoadState();
        LoggerInstance.Msg("Patches installed explicitly: " + HarmonyLib.Harmony.GetAllPatchedMethods().Count(m => HarmonyLib.Harmony.GetPatchInfo(m)?.Owners.Contains(HarmonyInstance.Id) == true));
        LoggerInstance.Msg("PUR save: " + SaveFile);
        LoggerInstance.Msg("Native save.sav is preserved. 20 permanent upgrades on one screen; ColdStorage disabled.");
    }

    internal static void LoadState()
    {
        try {
            Ranks = new int[Balance.Upgrades.Length]; Migrated = false; Loaded = true;
            if (!File.Exists(SaveFile)) return;
            var original = JObject.Parse(File.ReadAllText(SaveFile));
            var root = SaveMigration.Upgrade(original, out var scalingRefund,out var grinderRefund);
            root = RobotRetirement.Upgrade(root, out var robotRefund);
            if (!JToken.DeepEquals(original,root)) {
                var backup = SaveFile + ".before-v0.11.0";
                if (!File.Exists(backup)) File.Copy(SaveFile, backup);
                var temp = SaveFile + ".migration.tmp"; File.WriteAllText(temp, root.ToString());
                File.Replace(temp, SaveFile, SaveFile + ".bak");
                if (scalingRefund > 0) Self.LoggerInstance.Msg($"Scaling migration: refunded {scalingRefund} points.");
                if(grinderRefund>0)Self.LoggerInstance.Msg($"Grinder transfer: refunded {grinderRefund} dollars; old run nodes removed.");
                if (robotRefund > 0) Self.LoggerInstance.Msg($"Robot retirement: refunded {robotRefund} dollars to run money; retired robot IDs removed.");
            }
            if (root["pur"] is not JObject state) return;
            if ((int?)state["schema"] != 6) throw new InvalidDataException("Unknown PUR save schema");
            Ranks = state["ranks"]!.ToObject<int[]>()!;
            Balance.Validate(Ranks); Migrated = (bool?)state["migrated"] == true;
            Self.LoggerInstance.Msg("Loaded ranks: " + string.Join(",", Ranks));
        } catch (Exception ex) { Faulted = true; Self.LoggerInstance.Error(ex); }
    }

    internal static void WriteSave(SaveManager manager)
    {
        if (Faulted || !Loaded || manager.CurrentSaveData == null) return;
        Balance.Validate(Ranks);
        var root = JObject.Parse(JsonUtility.ToJson(manager.CurrentSaveData, true));
        root["pur"] = new JObject { ["schema"] = 6, ["migrated"] = Migrated, ["ranks"] = JArray.FromObject(Ranks) };
        var temp = SaveFile + ".tmp";
        File.WriteAllText(temp, root.ToString());
        if (File.Exists(SaveFile)) File.Replace(temp, SaveFile, SaveFile + ".bak");
        else File.Move(temp, SaveFile);
    }

    internal static void Migrate(PermanentUpgradeManager manager)
    {
        if (Faulted || Migrated || !Loaded) return;
        var save = SaveManager.Instance.CurrentSaveData;
        if (save == null || manager.nodes == null) return;
        var prices = new Dictionary<int, int>();
        foreach (var node in manager.nodes) if (node != null && node.skill != null) prices[(int)node.skill.skillId] = node.skill.price;
        var refund = 0;
        foreach (var id in save.unlockedPermanentUpgrades) {
            if (!prices.TryGetValue((int)id, out var price)) throw new InvalidDataException("Cannot refund unknown upgrade " + id);
            refund = checked(refund + price);
        }
        var oldPoints = save.upgradePoints;
        var oldIds = save.unlockedPermanentUpgrades;
        save.upgradePoints = checked(oldPoints + refund);
        save.unlockedPermanentUpgrades = new Il2CppSystem.Collections.Generic.List<SkillId>();
        Migrated = true;
        try { WriteSave(SaveManager.Instance); }
        catch { Migrated = false; save.upgradePoints = oldPoints; save.unlockedPermanentUpgrades = oldIds; throw; }
        Notice = $"Previous purchases refunded: {refund:N0} points. Original progress preserved.";
        Self.LoggerInstance.Msg($"Migration: refunded {refund}, balance {save.upgradePoints}; ranks zero.");
    }

    internal static void ApplyBonuses()
    {
        if (!Migrated || Faulted) return;
        Specials.Apply();
        var gm = GameManager.Instance;
        if(BonusOwner!=gm){
            GameBonuses.Clear();AmmoBonuses.Clear();IncomeBonuses.Clear();MoveBonuses.Clear();StorageBonuses.Clear();BaseStorage.Clear();
            BonusOwner=gm;
        }
        if (gm != null) {
            GameBonuses.TryGetValue(gm.Pointer, out var old);
            var damage = (float)Balance.Value(1, Ranks[1]) - 1;
            gm.damageMultiplier += damage - old;
            gm.coldStorage = false;
            GameBonuses[gm.Pointer] = damage;
            AmmoBonuses.TryGetValue(gm.Pointer,out var oldAmmo);var ammo=(float)Balance.Value(18,Ranks[18])-1;
            gm.ammoCapacityMultiplier+=ammo-oldAmmo;AmmoBonuses[gm.Pointer]=ammo;
        }
        var reward = RewardMachine.Instance;
        if (reward != null) {
            IncomeBonuses.TryGetValue(reward.Pointer, out var old);
            var bonus = (float)Balance.Value(2, Ranks[2]) - 1;
            reward.canValueMultiplier += bonus - old; IncomeBonuses[reward.Pointer] = bonus;
        }
        var player = Player.Instance;
        if (player != null) {
            MoveBonuses.TryGetValue(player.Pointer, out var old);
            var bonus = (float)Balance.Value(5, Ranks[5]) / 100;
            player.moveSpeedMultiplier += bonus - old; MoveBonuses[player.Pointer] = bonus;
        }
        foreach (var vacuum in Object.FindObjectsOfType<Vacuum>(true)) {
            if (!BaseStorage.TryGetValue(vacuum.Pointer, out var capacity)) BaseStorage[vacuum.Pointer] = capacity = vacuum.maxCapacity;
            StorageBonuses.TryGetValue(vacuum.Pointer, out var old);
            var bonus = (int)Math.Ceiling(capacity * (Balance.Value(4, Ranks[4]) - 1));
            vacuum.maxCapacity += bonus - old; StorageBonuses[vacuum.Pointer] = bonus;
        }
    }

    public override void OnSceneWasUnloaded(int buildIndex,string sceneName)
    {
        // Do not discard a new scene that Unity has already initialized during an additive transition.
        if(GameManager.Instance!=null)return;
        FactoryExtension.ResetScene();FactorySafety.ResetScene();IndustrialSuction.ResetScene();
        ConveyorStability.ResetScene();IntakeGeometry.ResetScene();NativeUi.ResetScene();Specials.ResetScene();
        GameBonuses.Clear();AmmoBonuses.Clear();IncomeBonuses.Clear();MoveBonuses.Clear();StorageBonuses.Clear();BaseStorage.Clear();BonusOwner=null;NextScan=0;
        Self.LoggerInstance.Msg("Released factory scene caches: "+sceneName);
    }

    public override void OnFixedUpdate() {var start=PerformanceAudit.Begin();IndustrialSuction.FixedTick();PerformanceAudit.End(0,start);start=PerformanceAudit.Begin();ConveyorStability.FixedTick();PerformanceAudit.End(1,start);}

    public override void OnUpdate()
    {
        PerformanceAudit.Tick();var auditStart=PerformanceAudit.Begin();try{
        FactoryExtension.Tick();
        NativeUi.ReadInput();
        Specials.Tick();
        FactorySafety.Tick();
        if (Time.unscaledTime < NextScan || Faulted) return;
        NextScan = Time.unscaledTime + .5f;
        try {
            var manager = PermanentUpgradeManager.Instance;
            if (manager == null || !Loaded) return;
            Migrate(manager);
            NativeUi.Ensure(manager);
            NativeUi.Refresh();
            ApplyBonuses();
        } catch (Exception ex) { Faulted = true; Self.LoggerInstance.Error(ex); }
        }finally{PerformanceAudit.End(2,auditStart);}
    }

    internal static bool CanShop() => !Faulted && Migrated && SaveManager.Instance?.CurrentSaveData != null;
    internal static void Purchase(int index)
    {
        if (!CanShop()) return;
        var save = SaveManager.Instance.CurrentSaveData; var rank = Ranks[index];
        if (!Balance.CanPurchase(index,Ranks) || rank >= Balance.Upgrades[index].Cap || save.upgradePoints < Balance.Price(index, rank)) return;
        var points = save.upgradePoints;
        save.upgradePoints -= Balance.Price(index, rank); Ranks[index]++;
        try { WriteSave(SaveManager.Instance); }
        catch (Exception ex) { save.upgradePoints = points; Ranks[index]--; Self.LoggerInstance.Error(ex); return; }
        ApplyBonuses(); PermanentUpgradeManager.Instance.UpdatePointsText();
        Self.LoggerInstance.Msg($"Purchased {Balance.Upgrades[index].Name} rank {Ranks[index]}; balance {save.upgradePoints}");
    }

    internal static void Respec()
    {
        if (!CanShop()) return;
        var save = SaveManager.Instance.CurrentSaveData; var previous = Ranks; var points = save.upgradePoints;
        save.upgradePoints = checked(points + Balance.Refund(Ranks)); Ranks = new int[Balance.Upgrades.Length];
        try { WriteSave(SaveManager.Instance); }
        catch (Exception ex) { Ranks = previous; save.upgradePoints = points; Self.LoggerInstance.Error(ex); return; }
        ApplyBonuses(); PermanentUpgradeManager.Instance.UpdatePointsText();
    }

    [HarmonyPatch(typeof(SaveManager), "get_SavePath")]
    static class SavePathPatch { static void Postfix(ref string __result) => __result = SaveFile; }
    [HarmonyPatch(typeof(SaveManager), "Load")]
    static class LoadPatch { static void Prefix() => LoadState(); }
    [HarmonyPatch(typeof(SaveManager), "Save")]
    static class SavePatch { static bool Prefix(SaveManager __instance) { if(BulkPurchase.Running)return false; try { WriteSave(__instance); } catch (Exception ex) { Faulted = true; Self.LoggerInstance.Error(ex); } return false; } }
    [HarmonyPatch(typeof(PermanentUpgradeManager), "ApplySave")]
    static class ApplyPatch { static bool Prefix(PermanentUpgradeManager __instance) { try { Migrate(__instance); ApplyBonuses(); __instance.UpdatePointsText(); } catch (Exception ex) { Faulted = true; Self.LoggerInstance.Error(ex); } return false; } }
    [HarmonyPatch(typeof(PermanentUpgrade), "OnClick")]
    static class OldPurchasePatch { static bool Prefix(PermanentUpgrade __instance) { NativeUi.Click(__instance); return false; } }
    [HarmonyPatch(typeof(ColdStorageSkill), "ApplySkill")]
    static class ColdStoragePatch { static bool Prefix() => false; }
    [HarmonyPatch(typeof(GameManager), "ResetToDefaultValues")]
    static class GameReset { static void Postfix(GameManager __instance) { GameBonuses.Remove(__instance.Pointer);AmmoBonuses.Remove(__instance.Pointer); __instance.coldStorage = false; } }
    [HarmonyPatch(typeof(Vacuum),"Update")]
    static class VacuumCadencePatch {
        static void Prefix(Vacuum __instance,out float __state){__state=__instance.throwInterval;if(Migrated&&!Faulted)__instance.throwInterval=__state/(float)Balance.Value(12,Ranks[12]);}
        static void Finalizer(Vacuum __instance,float __state){__instance.throwInterval=__state;}
    }
    [HarmonyPatch(typeof(CanProcessor), "GetCanInterval")]
    static class ProductionSpeedPatch { static void Postfix(ref float __result) { if(Migrated&&!Faulted)__result/=(float)Balance.Value(3,Ranks[3]); } }
    [HarmonyPatch(typeof(RewardMachine), "ResetToDefaultValues")]
    static class RewardReset { static void Postfix(RewardMachine __instance) => IncomeBonuses.Remove(__instance.Pointer); }
    [HarmonyPatch(typeof(Player), "ResetToDefaultValues")]
    static class PlayerReset { static void Postfix(Player __instance) => MoveBonuses.Remove(__instance.Pointer); }
    [HarmonyPatch(typeof(Vacuum), "ResetToDefaultValues")]
    static class VacuumReset { static void Postfix(Vacuum __instance) { StorageBonuses.Remove(__instance.Pointer); BaseStorage[__instance.Pointer] = __instance.maxCapacity; } }
    [HarmonyPatch(typeof(PermanentUpgradeManager), "Sell")]
    static class SellPatch { static void Postfix() { var mm = MoneyManager.Instance; if (mm != null) mm.SetMoney(mm.Money + (long)Balance.Value(0, Ranks[0])); SaveManager.Instance.Save(); } }
    [HarmonyPatch(typeof(PermanentUpgradeManager), "HasAllPermanentUpgrades")]
    static class CompletePatch { static bool Prefix(ref bool __result) { __result = Ranks.Select((r,i)=>r>=Balance.Upgrades[i].Cap).All(v=>v); return false; } }
}
