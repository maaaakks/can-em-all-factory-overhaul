using System.Diagnostics;
using HarmonyLib;
using Il2Cpp;
using UnityEngine;
namespace PermanentUpgradesRevamped;
internal static class PerformanceAudit
{
    internal readonly struct Stamp {public readonly long Ticks,Bytes;public Stamp(long ticks,long bytes){Ticks=ticks;Bytes=bytes;}}
    sealed class Metric {public long Ticks,Bytes,Max,Calls;public void Clear(){Ticks=Bytes=Max=Calls=0;}}
    static readonly Metric[] Metrics={new(),new(),new()};static readonly List<float> Frames=new(8192);
    static bool Active;static float Seconds;static long SoldUnits,Revenue;
    internal static Stamp Begin()=>Active?new Stamp(Stopwatch.GetTimestamp(),System.GC.GetAllocatedBytesForCurrentThread()):default;
    internal static void End(int section,Stamp start){if(start.Ticks==0)return;var ticks=Stopwatch.GetTimestamp()-start.Ticks;var m=Metrics[section];m.Ticks+=ticks;m.Max=Math.Max(m.Max,ticks);m.Bytes+=Math.Max(0,System.GC.GetAllocatedBytesForCurrentThread()-start.Bytes);m.Calls++;}
    internal static void Tick()
    {
        var gm=GameManager.Instance;var active=gm!=null&&!gm.IsPaused&&!gm.IsGameOver&&Time.timeScale>0&&(Menu.Instance==null||Menu.Instance.menuPanel==null||!Menu.Instance.menuPanel.activeInHierarchy);
        if(!active){Active=false;Seconds=0;Frames.Clear();foreach(var m in Metrics)m.Clear();SoldUnits=Revenue=0;return;}
        Active=true;var dt=Time.unscaledDeltaTime;if(dt<=0)return;Frames.Add(dt*1000);Seconds+=dt;if(Seconds<30)return;
        Frames.Sort();var average=Frames.Average();var p95=Frames[(int)((Frames.Count-1)*.95)];
        Mod.Self.LoggerInstance.Msg($"PERF active 30s: frame avg {average:0.0}ms (~{1000/average:0}fps), P95 {p95:0.0}ms, >50ms {Frames.Count(f=>f>50)}/{Frames.Count}; enemies {gm.enemies.Count}, physical cans {gm.PhysicalCanCount}/{gm.maxPhysicalCans}, guided {ConveyorStability.GuidedCount}.");
        var names=new[]{"suction","conveyors","mod update"};var tickMs=1000.0/Stopwatch.Frequency;
        for(var i=0;i<Metrics.Length;i++){var m=Metrics[i];if(m.Calls>0)Mod.Self.LoggerInstance.Msg($"PERF {names[i]}: mean {m.Ticks*tickMs/m.Calls:0.000}ms, max {m.Max*tickMs:0.000}ms, managed alloc {m.Bytes/1024.0/Seconds:0.0}KB/s.");}
        var reward=RewardMachine.Instance;var truck=Truck.Instance;
        var ground=0;foreach(var can in gm.cans)if(can!=null&&can.rb!=null&&can.gameObject.activeInHierarchy&&!can.IsProcessed&&!can.IsInPool&&!can.isHeld&&can.rb.position.y<.4f)ground++;
        var pending=0;foreach(var processor in gm.canProcessors)if(processor!=null)pending+=processor.pendingCans.Count;
        if(reward!=null&&truck!=null)Mod.Self.LoggerInstance.Msg($"ECONOMY 30s: sold units {SoldUnits}, earned {Revenue}; floor cans {ground}, pending production {pending}; value multiplier {reward.canValueMultiplier:0.##}, shiny value {reward.shinyValueMultiplier:0.##} x {reward.permanentShinyValueMultiplier:0.##}, cans per enemy {gm.cansPerEnemy}, native interval {gm.processInterval:0.###}, permanent processing x{Balance.Value(3,Mod.Ranks[3]):0.##}, random shiny {EffectsRules.Chance(truck.shinyProbability,(float)Balance.Value(6,Mod.Ranks[6])/100,.4f)*100:0.#}%.");
        Seconds=0;Frames.Clear();foreach(var m in Metrics)m.Clear();SoldUnits=Revenue=0;
    }
    struct SaleState{public long Money;public int Quantity;public bool Track;}
    [HarmonyPatch(typeof(RewardMachine),"RewardCan")]
    static class SalesPatch {
        static void Prefix(Can can,out SaleState __state){__state=Active&&can!=null&&MoneyManager.Instance!=null?new SaleState{Money=MoneyManager.Instance.Money,Quantity=can.quantity,Track=true}:default;}
        static void Postfix(SaleState __state){if(!__state.Track||MoneyManager.Instance==null)return;var delta=MoneyManager.Instance.Money-__state.Money;if(delta>0){Revenue+=delta;SoldUnits+=__state.Quantity;}}
    }
}
