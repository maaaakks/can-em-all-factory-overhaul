using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace PermanentUpgradesRevamped;

#pragma warning disable CS0649 // Populated from the embedded original-geometry JSON.
internal sealed class FloorMeshData
{
    public string Name,Kind;
    public float[][] Vertices,Normals,UV,UV2,Tangents;
    public int[][] Submeshes;
}
#pragma warning restore CS0649

internal static class IntakeGeometry
{
    internal sealed class Intake
    {
        public CanProcessor Processor;
        public float Front,Back,CenterZ,HalfWidth,FloorY;
        public Transform Group,CoverGroup;
        public Vector3 CoverBaseScale;
        public NavMeshObstacle Obstacle;
        public Vector3 Center=>new((Front+Back)/2,FloorY-1.6f,CenterZ);
        public Vector3 Extents=>new((Back-Front)/2-.07f,1.4f,HalfWidth*Width-.08f);
        public bool Contains(Vector3 p,float top=.55f)=>p.x>Front&&p.x<Back&&Math.Abs(p.z-CenterZ)<HalfWidth*Width-.05f&&p.y<FloorY+top&&p.y>FloorY-3;
    }
    internal static readonly List<Intake> Intakes=new();
    sealed class FloorPart {public FloorMeshData Data;public Mesh Mesh;public MeshCollider Collider;}
    static readonly List<FloorPart> Parts=new();
    static readonly HashSet<IntPtr> PlayerColliders=new();
    static IntPtr PlayerPointer;
    static bool Ready;
    internal static float Width=1;

    internal static void Ensure(GameManager gm)
    {
        if(Ready)return;
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("PermanentUpgradesRevamped.FloorGeometry.json") ?? throw new InvalidOperationException("Floor geometry resource missing");
        var data=JsonConvert.DeserializeObject<FloorMeshData[]>(new StreamReader(stream).ReadToEnd());
        var floor=data.Single(d=>d.Name=="ConcreteFloorCol");
        foreach(var processor in gm.canProcessors) {
            var point=processor.point.position;
            var near=floor.Vertices.Where(v=>Math.Abs(v[2]-point.z)>.8f&&Math.Abs(v[2]-point.z)<2.3f&&v[0]>-4.5f&&v[0]<-.3f).ToArray();
            if(near.Length<2)throw new InvalidOperationException("Cannot identify original floor opening for "+processor.name);
            var zs=near.Select(v=>v[2]).ToArray();
            var xs=floor.Vertices.Select(v=>v[0]).Distinct().ToArray();
            var intake=new Intake {Processor=processor,CenterZ=(zs.Min()+zs.Max())/2,HalfWidth=(zs.Max()-zs.Min())/2,Front=xs.Where(x=>x<point.x).Max(),Back=xs.Where(x=>x>point.x).Min(),FloorY=floor.Vertices[0][1]};
            if(intake.HalfWidth<1||intake.HalfWidth>2.5f||intake.Back-intake.Front<2||intake.Back-intake.Front>5)throw new InvalidOperationException("Unexpected opening geometry");
            var root=new GameObject("PUR_IntakeWidth");root.transform.SetParent(processor.transform,false);
            var local=processor.transform.InverseTransformPoint(new Vector3(point.x,point.y,intake.CenterZ));
            root.transform.localPosition=new Vector3(0,0,local.z);intake.Group=root.transform;
            foreach(var child in processor.GetComponentsInChildren<Transform>(true).ToArray()) {
                if(child.parent!=processor.transform)continue;
                if(child.name.StartsWith("Machine_Crusher")||child.name.StartsWith("VacuumIgnoreBounds"))child.SetParent(root.transform,true);
            }
            var obstacleGo=new GameObject("PUR_IntakeNavExclusion");obstacleGo.transform.position=new Vector3((intake.Front+intake.Back)/2,intake.FloorY,intake.CenterZ);
            obstacleGo.transform.SetParent(processor.transform,true);
            intake.Obstacle=obstacleGo.AddComponent<NavMeshObstacle>();intake.Obstacle.shape=NavMeshObstacleShape.Box;intake.Obstacle.carving=true;intake.Obstacle.carveOnlyStationary=true;intake.Obstacle.carvingTimeToStationary=.1f;
            intake.Obstacle.enabled=false;
            foreach(var unlock in gm.unlockMachineSkills) {
                if(unlock==null||unlock.machine==null||unlock.machineCover==null)continue;
                var machine=processor.gameObject;
                if(unlock.machine!=machine&&!processor.transform.IsChildOf(unlock.machine.transform))continue;
                var cover=unlock.machineCover;
                var coverGroup=new GameObject("PUR_ClosedIntakeWidth");WorldIdentity(coverGroup.transform,cover.transform.parent);coverGroup.transform.position=new Vector3((intake.Front+intake.Back)/2,intake.FloorY,intake.CenterZ);
                cover.transform.SetParent(coverGroup.transform,true);intake.CoverGroup=coverGroup.transform;intake.CoverBaseScale=coverGroup.transform.localScale;
                var support=new GameObject("PUR_ClosedIntakeSupport");support.layer=cover.layer;WorldIdentity(support.transform,cover.transform);
                support.transform.position=new Vector3((intake.Front+intake.Back)/2,intake.FloorY-.08f,intake.CenterZ);
                var box=support.AddComponent<BoxCollider>();box.size=new Vector3(intake.Back-intake.Front+.10f,.16f,intake.HalfWidth*2+.10f);
                Mod.Self.LoggerInstance.Msg("Closed intake synchronized: "+cover.name+"; cover and solid support follow native unlock visibility.");break;
            }
            Intakes.Add(intake);
        }
        foreach(var source in data) {
            var mesh=new Mesh();mesh.name="PUR_"+source.Name;mesh.subMeshCount=source.Submeshes.Length;
            mesh.vertices=source.Vertices.Select(v=>new Vector3(v[0],v[1],v[2])).ToArray();
            for(var i=0;i<source.Submeshes.Length;i++)mesh.SetTriangles(source.Submeshes[i],i);
            if(source.Normals!=null)mesh.normals=source.Normals.Select(v=>new Vector3(v[0],v[1],v[2])).ToArray();
            if(source.UV!=null)mesh.uv=source.UV.Select(v=>new Vector2(v[0],v[1])).ToArray();
            if(source.UV2!=null)mesh.uv2=source.UV2.Select(v=>new Vector2(v[0],v[1])).ToArray();
            if(source.Tangents!=null)mesh.tangents=source.Tangents.Select(v=>new Vector4(v[0],v[1],v[2],v[3])).ToArray();
            mesh.RecalculateBounds();var part=new FloorPart {Data=source,Mesh=mesh};
            if(source.Kind=="visual") {
                var original=Object.FindObjectsOfType<MeshRenderer>(true).Single(r=>r.name==source.Name);
                var replacement=new GameObject("PUR_ResizableFloor");WorldIdentity(replacement.transform,original.transform);replacement.layer=original.gameObject.layer;
                replacement.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=replacement.AddComponent<MeshRenderer>();renderer.sharedMaterials=original.sharedMaterials;renderer.lightmapIndex=original.lightmapIndex;renderer.lightmapScaleOffset=original.lightmapScaleOffset;
                original.enabled=false;
            }else {
                var original=Object.FindObjectsOfType<MeshCollider>(true).Single(c=>c.name==source.Name);
                var replacement=new GameObject("PUR_"+source.Name);WorldIdentity(replacement.transform,original.transform);replacement.layer=original.gameObject.layer;
                part.Collider=replacement.AddComponent<MeshCollider>();part.Collider.sharedMaterial=original.sharedMaterial;part.Collider.sharedMesh=mesh;original.enabled=false;
            }
            Parts.Add(part);
        }
        Ready=true;Apply(1,true);
        Mod.Self.LoggerInstance.Msg("Intake geometry: original visual floor and two collision floors reconstructed; five exact opening bounds identified.");
    }

    static void WorldIdentity(Transform target,Transform parent)
    {
        target.SetParent(parent,true);target.position=Vector3.zero;target.rotation=Quaternion.identity;
        var scale=parent.lossyScale;target.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
    }

    internal static void Apply(float width,bool force=false)
    {
        if(!Ready||(!force&&Math.Abs(Width-width)<.001f))return;
        Width=width;
        foreach(var part in Parts) {
            var vertices=part.Data.Vertices.Select(v=>new Vector3(v[0],v[1],v[2])).ToArray();
            for(var i=0;i<vertices.Length;i++) {
                var v=vertices[i];
                foreach(var intake in Intakes) {
                    if(v.x<intake.Front-.3f||v.x>intake.Back+.3f||Math.Abs(v.z-intake.CenterZ)>intake.HalfWidth+.15f)continue;
                    v.z=intake.CenterZ+(v.z-intake.CenterZ)*width;break;
                }
                vertices[i]=v;
            }
            part.Mesh.vertices=vertices;part.Mesh.RecalculateBounds();
            if(part.Collider!=null){part.Collider.sharedMesh=null;part.Collider.sharedMesh=part.Mesh;}
        }
        foreach(var intake in Intakes) {
            intake.Group.localScale=new Vector3(1,1,width);
            if(intake.CoverGroup!=null)intake.CoverGroup.localScale=new Vector3(intake.CoverBaseScale.x,intake.CoverBaseScale.y,intake.CoverBaseScale.z*width);
            intake.Obstacle.size=new Vector3(intake.Back-intake.Front+.15f,2,intake.HalfWidth*width*2+.15f);
            intake.Obstacle.enabled=width>1.01f;
        }
        Physics.SyncTransforms();Mod.Self.LoggerInstance.Msg($"Intake width ×{width}: floor visuals, floor colliders, pit geometry, rectangular capture and NavMesh exclusions synchronized.");
    }

    [HarmonyPatch(typeof(Physics),"OverlapSphereNonAlloc",new[]{typeof(Vector3),typeof(float),typeof(Il2CppReferenceArray<Collider>),typeof(int),typeof(QueryTriggerInteraction)})]
    static class CapturePatch
    {
        static bool Prefix(Vector3 position,Il2CppReferenceArray<Collider> results,int layerMask,QueryTriggerInteraction queryTriggerInteraction,ref int __result)
        {
            if(!Ready||Width<=1.01f||results==null)return true;
            foreach(var intake in Intakes) {
                if(intake.Processor.hits.Pointer!=results.Pointer)continue;
                var count=Physics.OverlapBoxNonAlloc(intake.Center,intake.Extents,results,Quaternion.identity,layerMask,queryTriggerInteraction);
                var player=Player.Instance;
                if(player!=null&&PlayerPointer!=player.Pointer){PlayerPointer=player.Pointer;PlayerColliders.Clear();foreach(var c in player.GetComponentsInChildren<Collider>(true))if(c.attachedRigidbody==player.rb)PlayerColliders.Add(c.Pointer);}
                var canHurtPlayer=player!=null&&intake.Contains(player.groundPoint!=null?player.groundPoint.position:player.transform.position,-.15f);
                var kept=0;
                for(var i=0;i<count;i++) {
                    var collider=results[i];if(collider==null)continue;
                    if(!canHurtPlayer&&PlayerColliders.Contains(collider.Pointer))continue;
                    results[kept++]=collider;
                }
                __result=kept;return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(RobotCleaner),"IsInsideProcessor")]
    static class InsidePatch{static void Postfix(Vector3 position,ref bool __result){if(Ready&&Width>1.01f)__result=Intakes.Any(i=>i.Processor.IsOn&&i.Contains(position));}}
}
