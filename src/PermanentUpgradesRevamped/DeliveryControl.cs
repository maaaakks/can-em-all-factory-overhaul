using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;
using Object=UnityEngine.Object;

namespace PermanentUpgradesRevamped;

internal static class DeliveryControl
{
    static GameManager Owner;
    static GameObject Root,Prompt;
    static Collider ButtonCollider;
    static Renderer ButtonRenderer;
    static Material PlateMaterial,ButtonMaterial;
    static TextMeshProUGUI Sign,PromptText;
    static AudioSource PausedEngine;
    static bool Paused,Failed,Waiting;
    static float NextTry,NextPress;

    internal static void ResetScene()
    {
        if(PausedEngine!=null)PausedEngine.UnPause();PausedEngine=null;
        if(Root!=null)Object.Destroy(Root);if(Prompt!=null)Object.Destroy(Prompt);
        if(PlateMaterial!=null)Object.Destroy(PlateMaterial);if(ButtonMaterial!=null)Object.Destroy(ButtonMaterial);
        Root=Prompt=null;Owner=null;ButtonCollider=null;ButtonRenderer=null;Sign=PromptText=null;
        PlateMaterial=ButtonMaterial=null;Paused=Failed=Waiting=false;NextTry=NextPress=0;
    }
    static GameObject Rect(string name,Transform parent)
    {
        var go=new GameObject(name,new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppSystem.Type>(new[]{Il2CppInterop.Runtime.Il2CppType.Of<RectTransform>()}));
        go.transform.SetParent(parent,false);return go;
    }
    static TextMeshProUGUI Text(Transform parent,TMP_Text template,string name)
    {
        var go=Rect(name,parent);var text=go.AddComponent<TextMeshProUGUI>();
        text.font=template.font;text.fontSharedMaterial=template.fontSharedMaterial;text.fontSize=24;
        text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.color=new Color(1,.92f,.77f);
        var rt=text.rectTransform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
        return text;
    }
    static void Build(Truck truck,TMP_Text template)
    {
        // Native gate spans Z around zero; mount inside the factory, clear of its moving panels.
        var gate=truck.facilityDoor.transform;
        Root=new GameObject("PUR_DeliveryPauseControl");Root.transform.position=gate.position+new Vector3(.32f,1.25f,-4.05f);
        PlateMaterial=new Material(Shader.Find("Standard")){name="PUR_DeliveryPausePlate",color=new Color(.12f,.15f,.17f)};
        ButtonMaterial=new Material(Shader.Find("Standard")){name="PUR_DeliveryPauseButton"};ButtonMaterial.EnableKeyword("_EMISSION");
        var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);plate.name="PUR_DeliveryControlPlate";
        plate.transform.SetParent(Root.transform,false);plate.transform.localScale=new Vector3(.14f,.60f,.55f);plate.GetComponent<Renderer>().sharedMaterial=PlateMaterial;
        var button=GameObject.CreatePrimitive(PrimitiveType.Cylinder);button.name="PUR_DeliveryToggleButton";
        button.transform.SetParent(Root.transform,false);button.transform.localPosition=new Vector3(.11f,0,0);
        button.transform.localRotation=Quaternion.Euler(0,0,90);button.transform.localScale=new Vector3(.30f,.065f,.30f);
        ButtonCollider=button.GetComponent<Collider>();ButtonRenderer=button.GetComponent<Renderer>();ButtonRenderer.sharedMaterial=ButtonMaterial;
        var board=Rect("PUR_DeliveryStatus",Root.transform);board.transform.localPosition=new Vector3(.10f,.65f,0);
        board.transform.localRotation=Quaternion.Euler(0,-90,0);board.transform.localScale=Vector3.one*.0025f;
        board.GetComponent<RectTransform>().sizeDelta=new Vector2(540,220);board.AddComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        Sign=Text(board.transform,template,"Status");Sign.fontSize=30;
        var hud=template.GetComponentInParent<Canvas>();
        Prompt=Rect("PUR_DeliveryPausePrompt",hud.transform);var rt=Prompt.GetComponent<RectTransform>();
        rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=new Vector2(0,-65);rt.sizeDelta=new Vector2(560,70);
        PromptText=Text(Prompt.transform,template,"Prompt");PromptText.fontSize=13;Prompt.SetActive(false);
        UpdateStatus();Mod.Self.LoggerInstance.Msg($"Delivery pause button placed beside native gate at {Root.transform.position}. E toggles; current delivery is allowed to finish.");
    }
    static void UpdateStatus()
    {
        if(Sign!=null)Sign.text=Paused?"LIVRAISONS\n1 PAR JOUR":"LIVRAISONS\nAUTORISÉES";
        var color=Paused?new Color(.9f,.15f,.08f):new Color(.12f,.78f,.24f);
        if(ButtonMaterial!=null){ButtonMaterial.color=color;ButtonMaterial.SetColor("_EmissionColor",color*.35f);}
    }
    static bool Aiming(GameManager gm)
    {
        if(gm.IsPaused||gm.IsGameOver||!gm.CanPlayerInput||NativeUi.IsOpen||Cursor.lockState!=CursorLockMode.Locked)return false;
        var camera=Player.Instance?.mainCamera;if(camera==null||ButtonCollider==null)return false;
        return Physics.Raycast(camera.transform.position,camera.transform.forward,out RaycastHit hit,2.7f,~0,QueryTriggerInteraction.Ignore)&&hit.collider==ButtonCollider;
    }
    internal static void Tick()
    {
        if(Mod.Faulted)return;
        try{
            var gm=GameManager.Instance;if(gm==null)return;
            if(Owner!=gm){ResetScene();Owner=gm;}
            if(Failed)return;
            if(Root==null){
                if(Time.unscaledTime<NextTry)return;NextTry=Time.unscaledTime+.5f;
                var truck=Truck.Instance;var template=MoneyManager.Instance?.moneyText;
                if(truck?.facilityDoor==null||template==null)return;Build(truck,template);
            }
            var aiming=Aiming(gm);Prompt.SetActive(aiming);
            if(!aiming)return;
            PromptText.text=Paused?"E · Autoriser toutes les livraisons": "E · Limiter à une livraison par jour";
            if(Paused&&!Waiting)PromptText.text+="\nLa livraison en cours se termine.";
            if(Input.GetKeyDown(KeyCode.E)&&Time.unscaledTime>=NextPress){
                Paused=!Paused;NextPress=Time.unscaledTime+.3f;UpdateStatus();
                Mod.Self.LoggerInstance.Msg(Paused?"Extra deliveries paused; first native delivery of every day remains enabled.":"All deliveries resumed by operator.");
            }
        }catch(Exception ex){Paused=false;Failed=true;if(Prompt!=null)Prompt.SetActive(false);Mod.Self.LoggerInstance.Error("Delivery button unavailable; native deliveries left enabled: "+ex);}
    }
    [HarmonyPatch(typeof(Truck._TruckUpdate_d__51),"MoveNext")]
    static class PauseBetweenVisits
    {
        static bool Prefix(Truck._TruckUpdate_d__51 __instance,ref bool __result)
        {
            if(Mod.Faulted||Failed||!DeliveryPauseRules.ShouldWait(Paused,__instance.__1__state,__instance._t_5__3)){
                if(PausedEngine!=null)PausedEngine.UnPause();PausedEngine=null;Waiting=false;return true;
            }
            // Preserve state, local variables and first-truck-of-day bookkeeping.
            // Yield one frame rather than terminating or creating another truck coroutine.
            var truck=__instance.__4__this;
            if(truck!=null&&__instance.__1__state==13){
                var parked=truck.startTruckPosition-Vector3.right*30;
                if(PausedEngine==null&&(truck.truck.position-parked).sqrMagnitude<=.04f&&truck.engineAudioSource!=null&&truck.engineAudioSource.isPlaying){PausedEngine=truck.engineAudioSource;PausedEngine.Pause();}
            }
            Waiting=true;__instance.__2__current=null;__result=true;return false;
        }
    }
}
