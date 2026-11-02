using Il2Cpp;
using UnityEngine;

namespace PermanentUpgradesRevamped;

internal static class IndustrialSuction
{
    sealed class Body{public Enemy Enemy;public GameObject GameObject;public Rigidbody Rigidbody;public Collider[] Colliders;public PhysicsMaterial[] Materials;public bool Sliding;public IntakeGeometry.Intake Target;}
    static readonly List<Body> Bodies=new();
    static readonly Dictionary<IntPtr,Body> PreviousBodies=new();
    static readonly List<IntakeGeometry.Intake> ActiveIntakes=new();
    static readonly HashSet<IntPtr> ActiveProcessors=new();
    static float NextScan;
    static float NextRoute,NextAudit;
    static readonly Dictionary<IntakeGeometry.Intake,int> QueueCounts=new();
    internal static bool PullLiving=false;
    internal static float Range,Speed;
    internal static int LastCount;
    static PhysicsMaterial SlideMaterial;
    static bool MaterialChecked,SawPull;
    static readonly HashSet<IntPtr> Pulled=new();

    static void Sliding(Body body,bool enabled)
    {
        if(SlideMaterial==null||body.Sliding==enabled)return;
        if(enabled) {
            if(body.Colliders==null){body.Colliders=body.Enemy.GetComponentsInChildren<Collider>(true).ToArray();body.Materials=new PhysicsMaterial[body.Colliders.Length];}
            for(var i=0;i<body.Colliders.Length;i++){if(body.Colliders[i]==null)continue;body.Materials[i]=body.Colliders[i].sharedMaterial;body.Colliders[i].sharedMaterial=SlideMaterial;}
        }else if(body.Colliders!=null) {
            for(var i=0;i<body.Colliders.Length;i++)if(body.Colliders[i]!=null&&body.Colliders[i].sharedMaterial==SlideMaterial)body.Colliders[i].sharedMaterial=body.Materials[i];
        }
        body.Sliding=enabled;
    }

    internal static void Configure()
    {
        var enabled=Mod.Migrated&&!Mod.Faulted&&Mod.Ranks[9]>0;
        var range=enabled?(float)Balance.Value(9,Mod.Ranks[9]):0;
        var speed=enabled?(float)Balance.Value(10,Mod.Ranks[10]):0;
        if(Math.Abs(Range-range)>.001f||Math.Abs(Speed-speed)>.001f) {
            Range=range;Speed=speed;NextScan=0;
            Mod.Self.LoggerInstance.Msg($"Industrial suction: range {Range:0.#}m; speed {Speed:0.##}m/s; living creatures {(PullLiving?"included":"excluded")}.");
        }
    }

    internal static void FixedTick()
    {
        var gm=GameManager.Instance;
        if(Range<=0||Speed<=0||gm==null||Time.timeScale==0||gm.IsPaused||gm.IsGameOver){foreach(var body in Bodies)Sliding(body,false);return;}
        if(!MaterialChecked) {
            MaterialChecked=true;SlideMaterial=Resources.FindObjectsOfTypeAll<PhysicsMaterial>().FirstOrDefault(m=>m.name=="Slippery");
            Mod.Self.LoggerInstance.Msg("Suction sliding material: "+(SlideMaterial!=null?"native Slippery available":"not loaded; retaining native friction"));
        }
        if(Time.time>=NextScan) {
            NextScan=Time.time+.3f;PreviousBodies.Clear();foreach(var body in Bodies)if(body.Enemy!=null)PreviousBodies[body.Enemy.Pointer]=body;
            Bodies.Clear();
            foreach(var enemy in gm.enemies){
                if(enemy==null||(!enemy.IsDead&&!PullLiving)||enemy.rb==null)continue;
                var go=enemy.gameObject;if(!go.activeInHierarchy)continue;
                if(PreviousBodies.TryGetValue(enemy.Pointer,out var existing)){Bodies.Add(existing);PreviousBodies.Remove(enemy.Pointer);}
                else Bodies.Add(new Body{Enemy=enemy,GameObject=go,Rigidbody=enemy.rb});
            }
            foreach(var body in PreviousBodies.Values)Sliding(body,false);PreviousBodies.Clear();
        }
        ActiveIntakes.Clear();ActiveProcessors.Clear();foreach(var intake in IntakeGeometry.Intakes)if(Available(intake)){ActiveIntakes.Add(intake);ActiveProcessors.Add(intake.Processor.Pointer);}
        if(Time.time>=NextRoute){NextRoute=Time.time+1;RouteBodies();}
        var count=0;Pulled.Clear();
        foreach(var body in Bodies) {
            var enemy=body.Enemy;var rb=body.Rigidbody;
            if(enemy==null||rb==null||!SuctionRules.Eligible(body.GameObject.activeInHierarchy,enemy.IsDead,enemy.isHeld,enemy.IsFreeFall,rb.isKinematic,PullLiving))continue;
            var p=rb.position;
            var nearest=body.Target;
            if(nearest==null||!ActiveProcessors.Contains(nearest.Processor.Pointer)||p.y<nearest.FloorY-.6f||p.y>nearest.FloorY+2.5f)continue;
            var destination=Destination(nearest,p);var distance=(destination-p).sqrMagnitude;
            if(distance<.04f||distance>Range*Range)continue;
            var velocity=rb.linearVelocity;
            if(velocity.sqrMagnitude>100)continue; // Let freshly thrown bodies finish their flight.
            var direction=destination-p;direction.y=0;direction=direction.normalized;
            var desired=direction*Speed;
            SuctionRules.Acceleration(velocity.x,velocity.z,desired.x,desired.z,out var ax,out var az);
            var acceleration=new Vector3(ax,0,az);
            Sliding(body,true);Pulled.Add(enemy.Pointer);rb.AddForce(acceleration,ForceMode.Acceleration);count++;
        }
        foreach(var body in Bodies)if(body.Enemy==null||!Pulled.Contains(body.Enemy.Pointer))Sliding(body,false);
        if(count>0&&!SawPull){SawPull=true;Mod.Self.LoggerInstance.Msg($"Suction physics active on {count} eligible bodies; progressive horizontal force, gravity preserved.");}
        LastCount=count;
        if(count>0&&Time.time>=NextAudit) {
            NextAudit=Time.time+60;
            Mod.Self.LoggerInstance.Msg("Suction routing: "+string.Join("; ",IntakeGeometry.Intakes.Select(i=>$"{i.Processor.name}: active={Available(i)}, z={i.CenterZ:0.0}, queued={QueueCounts.GetValueOrDefault(i)}, inbound={Bodies.Count(b=>b.Enemy!=null&&b.Enemy.IsDead&&!b.Enemy.IsFreeFall&&b.Target==i)}")));
        }
    }

    static bool Available(IntakeGeometry.Intake intake)=>intake.Processor!=null&&intake.Processor.gameObject.activeInHierarchy&&intake.Processor.IsOn;
    static Vector3 Destination(IntakeGeometry.Intake intake,Vector3 p)
    {
        var half=Math.Max(.3f,intake.HalfWidth*IntakeGeometry.Width-.4f);
        return new Vector3((intake.Front+intake.Back)/2,p.y,Math.Clamp(p.z,intake.CenterZ-half,intake.CenterZ+half));
    }
    static void RouteBodies()
    {
        var loads=new Dictionary<IntakeGeometry.Intake,int>();foreach(var intake in IntakeGeometry.Intakes)loads.Add(intake,0);
        var eligible=Bodies.Where(b=>b.Enemy!=null&&b.Rigidbody!=null&&SuctionRules.Eligible(b.Enemy.gameObject.activeInHierarchy,b.Enemy.IsDead,b.Enemy.isHeld,b.Enemy.IsFreeFall,b.Rigidbody.isKinematic,PullLiving)).OrderBy(b=>b.Enemy.Pointer.ToInt64()).ToArray();
        QueueCounts.Clear();
        // Include bodies already falling or being processed: they still occupy this intake's queue.
        foreach(var body in Bodies) {
            var enemy=body.Enemy;if(enemy==null||body.Rigidbody==null||!body.GameObject.activeInHierarchy||!enemy.IsDead||enemy.isHeld)continue;
            var p=body.Rigidbody.position;
            foreach(var intake in ActiveIntakes) {
                if(p.y>intake.FloorY-3&&p.y<intake.FloorY+3.5f&&p.x>intake.Front-2&&p.x<intake.Back+1&&Math.Abs(p.z-intake.CenterZ)<intake.HalfWidth*IntakeGeometry.Width+1)loads[intake]++;
            }
        }
        foreach(var intake in IntakeGeometry.Intakes)QueueCounts[intake]=loads[intake];
        foreach(var body in eligible) {
            var p=body.Rigidbody.position;var old=body.Target;body.Target=null;var best=float.MaxValue;
            foreach(var intake in ActiveIntakes) {
                if(p.y<intake.FloorY-.6f||p.y>intake.FloorY+2.5f)continue;
                var distance=(Destination(intake,p)-p).magnitude;
                if(distance>Range)continue;
                // Finish entering an opening instead of dragging bodies back out to another queue.
                if(intake.Contains(p,2.5f)){body.Target=intake;break;}
                var score=SuctionRules.RouteScore(distance,loads[intake],old==intake);
                if(score>=best)continue;best=score;body.Target=intake;
            }
            if(body.Target!=null)loads[body.Target]++;
        }
    }
}
