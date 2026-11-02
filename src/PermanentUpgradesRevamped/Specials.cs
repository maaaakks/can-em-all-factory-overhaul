using HarmonyLib;
using Il2Cpp;
using UnityEngine;
namespace PermanentUpgradesRevamped;
internal static class Specials
{
    static bool Enabled=>Mod.Migrated&&!Mod.Faulted;
    static float Bonus(int index)=>(float)Balance.Value(index,Mod.Ranks[index]);
    static int SeenDay=-1;static bool DeliverySeen;static Enemy[] Pending;static float WaveFraction;
    static readonly Dictionary<IntPtr,float> StartingWaves=new();
    static float WaveScope;
    static readonly DailyDeliveryTracker FirstDelivery=new();
    internal static void Apply()
    {
        if(!Enabled)return;
        var gm=GameManager.Instance;if(gm!=null){gm.hasStartingShockwave=false;gm.firstBlood=Mod.Ranks[16]>0;}
        var truck=Truck.Instance;if(truck!=null){truck.shinyShipment=Mod.Ranks[13]>0;truck.damagedDelivery=Mod.Ranks[14]>0;}
        var reward=RewardMachine.Instance;if(reward!=null)reward.permanentShinyValueMultiplier=reward.defaultPermanentShinyValueMultiplier*Bonus(7);
    }
    internal static string FinalChance(int index)
    {
        if(index==6&&Truck.Instance!=null)return $"final : {EffectsRules.Chance(Truck.Instance.shinyProbability,Bonus(6)/100,.4f)*100:0.#} %";
        var gm=GameManager.Instance;
        if(gm!=null&&index==11)return $"final : {EffectsRules.Chance(gm.criticProbability*gm.criticProbabilityMultiplier,Bonus(11)/100,.5f)*100:0.#} %";
        if(gm!=null&&index==17)return $"final : {EffectsRules.Chance(gm.instantDeathProbability*gm.instantDeathProbabilityMultiplier,Bonus(17)/100,.05f)*100:0.#} %";
        return "";
    }
    internal static void Tick()
    {
        var save=SaveManager.Instance?.CurrentSaveData;
        if(save==null)return;
        if(save.day!=SeenDay){SeenDay=save.day;DeliverySeen=false;Pending=null;}
        var gm=GameManager.Instance;
        if(Pending==null||gm==null||gm.IsPaused||gm.IsGameOver||Time.timeScale<=0)return;
        foreach(var enemy in Pending)if(enemy!=null&&enemy.gameObject.activeInHierarchy&&!enemy.IsDead&&enemy.CurrentState!=Enemy.State.Roaming)return;
        var bodies=Pending.Where(e=>e!=null&&e.gameObject.activeInHierarchy&&!e.IsDead).ToArray();Pending=null;
        if(bodies.Length==0||ParticlePoolManager.Instance==null)return;
        var center=Vector3.zero;foreach(var e in bodies)center+=e.transform.position;center/=bodies.Length;
        var radius=1f;foreach(var e in bodies)radius=Math.Max(radius,Vector3.Distance(center,e.transform.position)+2);
        var wave=ParticlePoolManager.Instance.SpawnShockwave(center,radius,0);
        if(wave!=null){StartingWaves[wave.Pointer]=WaveFraction;Mod.Self.LoggerInstance.Msg($"Opening wave: {WaveFraction*100:0}% maximum HP, first delivery of day {SeenDay}.");}
    }
    [HarmonyPatch(typeof(Truck),"ReleaseEnemies")]
    static class DeliveryPatch {static void Postfix(Truck __instance){if(!Enabled)return;var day=SaveManager.Instance.CurrentSaveData.day;if(day!=SeenDay){SeenDay=day;DeliverySeen=false;}if(DeliverySeen)return;DeliverySeen=true;if(Mod.Ranks[15]>0){Pending=__instance.spawnedEnemies.ToArray();WaveFraction=Bonus(15)/100;}}}
    [HarmonyPatch(typeof(GameManager),"ResetToDefaultValues")]
    static class ResetPatch {static void Postfix(){SeenDay=-1;DeliverySeen=false;Pending=null;FirstDelivery.Reset();}}
    [HarmonyPatch(typeof(Shockwave),"DamageEnemies")]
    static class WaveScopePatch {
        static void Prefix(Shockwave __instance,out float __state){__state=WaveScope;WaveScope=StartingWaves.TryGetValue(__instance.Pointer,out var fraction)?fraction:0;}
        static void Finalizer(float __state){WaveScope=__state;}
    }
    [HarmonyPatch(typeof(Shockwave),"OnDisable")]
    static class WavePoolPatch {static void Prefix(Shockwave __instance)=>StartingWaves.Remove(__instance.Pointer);}
    [HarmonyPatch(typeof(Enemy),"ShockwaveDamage")]
    static class WaveDamagePatch {static void Prefix(Enemy __instance,ref float damage){if(WaveScope>0)damage=__instance.maxHealth*WaveScope;}}
    [HarmonyPatch(typeof(Enemy),"ApplyDamagedDelivery")]
    static class FragilePatch {static bool Prefix(Enemy __instance){if(!Enabled)return true;var fraction=Bonus(14)/100;__instance.health=__instance.maxHealth*(1-fraction);__instance.SetGlobalDamageVisual(fraction);return false;}}
    struct SpawnState {public float Shiny;public int Count,Max,Day,Extra;public bool First;}
    [HarmonyPatch(typeof(Truck),"SpawnEnemiesInsideTruck")]
    static class SpawnPatch {
        static void Prefix(Truck __instance,out SpawnState __state){
            __state=new SpawnState{Shiny=__instance.shinyProbability,Count=__instance.enemyCount,Max=__instance.maxEnemyCount};
            if(!Enabled||SaveManager.Instance?.CurrentSaveData==null)return;
            __instance.shinyProbability=EffectsRules.Chance(__state.Shiny,Bonus(6)/100,.4f);
            __state.Day=SaveManager.Instance.CurrentSaveData.day;__state.First=FirstDelivery.IsFirst(__state.Day);
            if(!__state.First)return;
            __state.Extra=FirstDeliveryRules.ExtraMonsters(Mod.Ranks[19]);
            __instance.enemyCount=FirstDeliveryRules.SpawnCount(__state.Count,Mod.Ranks[19]);
            __instance.maxEnemyCount=Math.Max(__state.Max,__instance.enemyCount);
        }
        static void Finalizer(Truck __instance,SpawnState __state,Exception __exception){
            __instance.shinyProbability=__state.Shiny;__instance.enemyCount=__state.Count;__instance.maxEnemyCount=__state.Max;
            if(__exception==null&&__state.First){FirstDelivery.Complete(__state.Day);if(__state.Extra>0)Mod.Self.LoggerInstance.Msg($"First delivery of day {__state.Day}: +{__state.Extra} monsters; normal {__state.Count}, spawned {__instance.spawnedEnemies.Count}; ordinary truck count restored.");}
        }
    }
    struct HitState {public float Crit,CritMultiplier,Death,DeathMultiplier;}
    [HarmonyPatch(typeof(GameManager),"OnHitEnemy")]
    static class HitPatch {
        static void Prefix(GameManager __instance,float probabilityMultiplier,out HitState __state){
            __state=new HitState{Crit=__instance.criticProbability,CritMultiplier=__instance.criticProbabilityMultiplier,Death=__instance.instantDeathProbability,DeathMultiplier=__instance.instantDeathProbabilityMultiplier};
            if(!Enabled)return;
            __instance.criticProbability=EffectsRules.PerRoll(__state.Crit*__state.CritMultiplier,Bonus(11)/100,.5f,probabilityMultiplier);__instance.criticProbabilityMultiplier=1;
            __instance.instantDeathProbability=EffectsRules.PerRoll(__state.Death*__state.DeathMultiplier,Bonus(17)/100,.05f,probabilityMultiplier);__instance.instantDeathProbabilityMultiplier=1;
        }
        static void Finalizer(GameManager __instance,HitState __state){__instance.criticProbability=__state.Crit;__instance.criticProbabilityMultiplier=__state.CritMultiplier;__instance.instantDeathProbability=__state.Death;__instance.instantDeathProbabilityMultiplier=__state.DeathMultiplier;}
    }
}
