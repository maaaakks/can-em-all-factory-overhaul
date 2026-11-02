using HarmonyLib;
using Il2Cpp;
using UnityEngine;
using Object=UnityEngine.Object;
namespace PermanentUpgradesRevamped;
internal static class ConveyorStability
{
    sealed class Belt{public ConveyorSimple Conveyor;public BoxCollider Collider;public Vector3 Forward;public Bounds Bounds;public Vector3 Min,Max,Center,Side;public float Speed;}
    sealed class Guide{public Can Can;public PhysicsMaterial Material;public CollisionDetectionMode Collision;public RigidbodyInterpolation Interpolation;public int Solver,VelocitySolver;}
    static readonly List<Belt> Belts=new();static readonly Dictionary<IntPtr,Guide> Guided=new();
    static readonly List<Belt> ActiveBelts=new();static readonly List<IntPtr> ReleaseIds=new();
    internal static int GuidedCount=>Guided.Count;
    static readonly HashSet<IntPtr> Seen=new();static bool Failed,Logged,Prepared;static PhysicsMaterial Material;
    static bool Register(ConveyorSimple conveyor)
    {
        if(conveyor==null||conveyor.rBody==null)return false;
        if(Belts.Any(b=>b.Conveyor==conveyor))return true;
        var collider=conveyor.GetComponent<BoxCollider>();if(collider==null)return false;
        Belts.Add(new Belt{Conveyor=conveyor,Collider=collider,Forward=conveyor.transform.forward*(conveyor.flip?-1:1)});return true;
    }
    static void Release(IntPtr id)
    {
        if(!Guided.TryGetValue(id,out var state))return;Guided.Remove(id);var can=state.Can;if(can==null||can.rb==null)return;
        if(can.mainCollider!=null&&can.mainCollider.sharedMaterial==Material)can.mainCollider.sharedMaterial=state.Material;
        can.rb.collisionDetectionMode=state.Collision;can.rb.interpolation=state.Interpolation;can.rb.solverIterations=state.Solver;can.rb.solverVelocityIterations=state.VelocitySolver;
    }
    internal static void FixedTick()
    {
        if(Failed)return;
        try{
            var gm=GameManager.Instance;if(gm==null||gm.IsPaused||Time.timeScale==0)return;
            if(Belts.Count==0)foreach(var conveyor in Object.FindObjectsOfType<ConveyorSimple>(true))Register(conveyor);
            if(!Prepared&&Belts.Count>0){Prepared=true;Mod.Self.LoggerInstance.Msg($"Conveyor stabilization prepared: {Belts.Count} belt colliders registered; native teleporting motor replaced by can guidance.");}
            if(Material==null){Material=new PhysicsMaterial();Material.name="PUR_ConveyorNoBounce";Material.bounciness=0;Material.bounceCombine=PhysicsMaterialCombine.Minimum;Material.staticFriction=.15f;Material.dynamicFriction=.15f;Material.frictionCombine=PhysicsMaterialCombine.Minimum;}
            ActiveBelts.Clear();
            foreach(var belt in Belts){if(belt.Collider==null||!belt.Collider.enabled||!belt.Conveyor.gameObject.activeInHierarchy)continue;belt.Speed=belt.Conveyor.speed;if(belt.Speed<=0)continue;belt.Bounds=belt.Collider.bounds;belt.Min=belt.Bounds.min;belt.Max=belt.Bounds.max;belt.Center=belt.Bounds.center;belt.Side=Vector3.Cross(Vector3.up,belt.Forward);ActiveBelts.Add(belt);}
            Seen.Clear();
            foreach(var can in gm.cans){
                if(can==null||can.rb==null||can.mainCollider==null||!can.gameObject.activeInHierarchy||can.IsInPool||can.IsProcessed||can.IsFreeFall||can.isHeld||can.rb.isKinematic)continue;
                var rb=can.rb;var p=rb.position;Belt selected=null;var best=float.MaxValue;
                foreach(var belt in ActiveBelts){
                    var min=belt.Min;var max=belt.Max;
                    if(p.y<max.y-.03f||p.y>max.y+.65f||p.x<min.x-.04f||p.x>max.x+.04f||p.z<min.z-.04f||p.z>max.z+.04f)continue;
                    var error=Vector3.Dot(belt.Center-p,belt.Side);var score=Math.Abs(error);
                    if(belt.Forward.x>.5f)score-=.22f; // At merging junctions, feed the outgoing horizontal lane.
                    if(score>=best)continue;best=score;selected=belt;
                }
                if(selected==null||!Guided.ContainsKey(can.Pointer)&&rb.linearVelocity.sqrMagnitude>225)continue;
                if(!Guided.ContainsKey(can.Pointer)){
                    Guided.Add(can.Pointer,new Guide{Can=can,Material=can.mainCollider.sharedMaterial,Collision=rb.collisionDetectionMode,Interpolation=rb.interpolation,Solver=rb.solverIterations,VelocitySolver=rb.solverVelocityIterations});
                    rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.solverIterations=Math.Max(12,rb.solverIterations);rb.solverVelocityIterations=Math.Max(6,rb.solverVelocityIterations);can.mainCollider.sharedMaterial=Material;
                }
                Seen.Add(can.Pointer);
                var side=selected.Side;var correction=Math.Clamp(Vector3.Dot(selected.Center-p,side)*10,-2,2);
                var velocity=selected.Forward*selected.Speed+side*correction;velocity.y=Math.Clamp(rb.linearVelocity.y,-2,.1f);rb.linearVelocity=velocity;
                var angular=rb.angularVelocity;rb.angularVelocity=new Vector3(Math.Clamp(angular.x,-1,1),angular.y*.8f,Math.Clamp(angular.z,-1,1));
            }
            ReleaseIds.Clear();foreach(var id in Guided.Keys)if(!Seen.Contains(id))ReleaseIds.Add(id);foreach(var id in ReleaseIds)Release(id);
            if(Guided.Count>0&&!Logged){Logged=true;Mod.Self.LoggerInstance.Msg($"Stable conveyor transport active on {Guided.Count} cans; {Belts.Count} native lanes, bought speeds preserved, continuous collision and no bounce.");}
        }catch(Exception ex){Failed=true;foreach(var id in Guided.Keys.ToArray())Release(id);Mod.Self.LoggerInstance.Error("Conveyor guide stopped; native conveyor physics restored: "+ex);}
    }
    [HarmonyPatch(typeof(ConveyorSimple),"FixedUpdate")]
    static class BeltPatch{static bool Prefix(ConveyorSimple __instance)=>Failed||!Register(__instance);}
    [HarmonyPatch(typeof(Can),"OnGrabbed")]
    static class GrabPatch{static void Prefix(Can __instance)=>Release(__instance.Pointer);}
    [HarmonyPatch(typeof(Can),"OnThrown")]
    static class ThrowPatch{static void Prefix(Can __instance)=>Release(__instance.Pointer);}
    [HarmonyPatch(typeof(Can),"OnReturnedToPool")]
    static class PoolPatch{static void Prefix(Can __instance)=>Release(__instance.Pointer);}
}
