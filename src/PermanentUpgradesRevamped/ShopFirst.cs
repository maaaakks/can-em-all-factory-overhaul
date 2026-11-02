using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace PermanentUpgradesRevamped;

internal static class ShopFirst
{
    static readonly PreparationGate Gate=new();
    static IntPtr Loop;
    internal static void ResetScene(){Loop=IntPtr.Zero;Gate.Reset();}

    static bool Blocked(GameManager gm)
    {
        var permanent=PermanentUpgradeManager.Instance?.permanentUpgradePanel;
        return NativeUi.IsOpen||gm.IsPaused||
            (permanent!=null&&permanent.gameObject.activeInHierarchy&&permanent.alpha>.05f)||
            (gm.pauseMenu!=null&&gm.pauseMenu.gameObject.activeInHierarchy&&gm.pauseMenu.alpha>.05f);
    }

    static void ShowShop(GameManager gm)
    {
        // Match the native between-day shop, without advancing its day counter, clock or truck loop.
        gm.IsGameOver=true;gm.IsPaused=false;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        gm.skillTreeView.transform.parent.gameObject.SetActive(true);
        gm.OnClickButtonPanel(gm.skillTreePanel);gm.skillTreeView.AnimateTree();
        if(gm.fadeImage!=null){var color=gm.fadeImage.color;color.a=0;gm.fadeImage.color=color;}
    }

    [HarmonyPatch(typeof(GameManager._GameLoop_d__178),"MoveNext")]
    static class InitialDayPatch
    {
        static bool Prefix(GameManager._GameLoop_d__178 __instance,ref bool __result)
        {
            if(__instance.__1__state!=0)return true;
            var gm=__instance.__4__this;if(gm==null||Mod.Faulted)return true;
            try{
                if(Loop!=__instance.Pointer||!Gate.Waiting){
                    Loop=__instance.Pointer;Gate.Begin();ShowShop(gm);
                    Mod.Self.LoggerInstance.Msg("Pre-day shop opened: saved day and first delivery have not advanced. Use NEXT DAY to begin.");
                }
                if(Input.GetKeyDown(KeyCode.Space))Gate.RequestStart(Blocked(gm));
                if(Gate.TryFinish(Blocked(gm))){
                    Mod.Self.LoggerInstance.Msg("Pre-day shopping complete; starting the native day loop.");return true;
                }
                __instance.__2__current=null;__result=true;return false;
            }catch(Exception ex){Gate.Reset();Mod.Self.LoggerInstance.Error("Pre-day shop unavailable; continuing native start: "+ex);return true;}
        }
    }

    [HarmonyPatch(typeof(GameManager),"NextDay")]
    static class StartButtonPatch
    {
        static bool Prefix(GameManager __instance)
        {
            if(!Gate.Waiting)return true;
            Gate.RequestStart(Blocked(__instance));return false;
        }
    }

    [HarmonyPatch(typeof(PermanentUpgradeManager),"Sell")]
    static class SalePatch
    {
        [HarmonyPriority(Priority.Last)]
        static void Postfix(PermanentUpgradeManager __instance)
        {
            try{
                var gm=GameManager.Instance;if(gm==null)return;
                // Preserve the native reset and our starting-cash grant, then return to the run shop.
                __instance.permanentUpgradePanel.gameObject.SetActive(false);
                ShowShop(gm);
                Mod.Self.LoggerInstance.Msg("Factory sold: run shop reopened before the next day; prestige remains accessible with P.");
            }catch(Exception ex){Mod.Self.LoggerInstance.Error("Post-sale shop unavailable: "+ex);}
        }
    }
}
