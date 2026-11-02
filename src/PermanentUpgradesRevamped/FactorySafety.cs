using HarmonyLib;
using Il2Cpp;
using UnityEngine;
using UnityEngine.Rendering;
namespace PermanentUpgradesRevamped;
internal static class FactorySafety
{
    static readonly List<Light> NightLights=new();static bool Failed,Saved,WasNight,Applied;
    static AmbientMode DayMode;static Color DayAmbient;static float DayIntensity;
    internal static void Tick()
    {
        if(Failed)return;
        try{
            var lighting=LightingSwitcher.Instance;if(lighting==null)return;
            if(NightLights.Count==0){
                foreach(var z in new[]{-21f,-10.5f,0f,10.5f,21f}){var go=new GameObject("PUR_BlueNightLight");go.transform.SetParent(lighting.transform,false);go.transform.position=new Vector3(-9,7,z);var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.22f,.43f,1);light.intensity=3;light.range=23;light.shadows=LightShadows.None;light.enabled=false;NightLights.Add(light);}
                Mod.Self.LoggerInstance.Msg("Factory safety: five blue night lights prepared; weapon hit impulse reduced to 30%, capped at 4 m/s added velocity.");
            }
            var night=!lighting.IsDay;
            if(!Saved){DayMode=RenderSettings.ambientMode;DayAmbient=RenderSettings.ambientLight;DayIntensity=RenderSettings.ambientIntensity;Saved=true;}
            if(Applied&&night==WasNight)return;
            Applied=true;
            foreach(var light in NightLights)light.enabled=night;
            if(night){RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.08f,.14f,.29f);RenderSettings.ambientIntensity=1;}
            else if(WasNight){RenderSettings.ambientMode=DayMode;RenderSettings.ambientLight=DayAmbient;RenderSettings.ambientIntensity=DayIntensity;}
            if(night!=WasNight)Mod.Self.LoggerInstance.Msg(night?"Blue night lighting enabled.":"Native daytime ambient restored.");WasNight=night;
        }catch(Exception ex){Failed=true;Mod.Self.LoggerInstance.Error("Factory night lighting stopped: "+ex);}
    }
    struct HitState{public Rigidbody Body;public Vector3 Velocity,Angular;}
    static HitState Before(Enemy enemy)=>enemy!=null&&enemy.rb!=null&&!enemy.isHeld?new HitState{Body=enemy.rb,Velocity=enemy.rb.linearVelocity,Angular=enemy.rb.angularVelocity}:default;
    static void After(HitState state)
    {
        if(state.Body==null||state.Body.isKinematic)return;
        var delta=state.Body.linearVelocity-state.Velocity;PhysicsRules.ReducedImpulse(delta.x,delta.y,delta.z,out var x,out var y,out var z);
        state.Body.linearVelocity=state.Velocity+new Vector3(x,y,z);state.Body.angularVelocity=state.Angular+(state.Body.angularVelocity-state.Angular)*.3f;
    }
    [HarmonyPatch(typeof(Enemy),"Damage")]
    static class HitPatch{static void Prefix(Enemy __instance,out HitState __state)=>__state=Before(__instance);static void Postfix(HitState __state)=>After(__state);}
    [HarmonyPatch(typeof(Enemy),"ShockwaveDamage")]
    static class WaveHitPatch{static void Prefix(Enemy __instance,out HitState __state)=>__state=Before(__instance);static void Postfix(HitState __state)=>After(__state);}
}
