using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace PermanentUpgradesRevamped;

internal static class NativeUi
{
    static readonly Dictionary<IntPtr, int> Actions = new();
    static readonly List<PermanentUpgrade> Cards = new();
    static readonly List<TextMeshProUGUI> Bonuses = new(), RankLabels = new();
    static PermanentUpgradeManager Manager;
    static PermanentUpgrade Access, Close, Refund, BulkBuy;
    static readonly List<Image> Progress=new();
    static readonly Color PanelGreen=new(.075f,.15f,.13f,1),RowGreen=new(.12f,.23f,.20f,1);
    static TextMeshProUGUI Tooltip;
    static readonly List<TextMeshProUGUI> GroupHeaders=new();
    static GameObject Root;
    static int LastBulkVisible=-1;
    static bool Opened, PreviousActive, PreviousPaused, ConfirmRefund;
    static float PreviousAlpha, PreviousTime;
    static CursorLockMode PreviousCursor;
    static bool PreviousCursorVisible;
    static readonly List<GameObject> TemporarilyHidden = new();
    static readonly Color Cream = new(.90f,.88f,.76f,1);
    static readonly Color Orange = new(1,.66f,.12f,1);

    internal static void Ensure(PermanentUpgradeManager manager)
    {
        if (Root != null && Manager == manager) return;
        if (manager.nodes == null || manager.nodes.Length < 1) return;
        Manager = manager; Cards.Clear(); Bonuses.Clear(); RankLabels.Clear(); Progress.Clear();Actions.Clear();
        var panel = manager.permanentUpgradePanel.transform;
        Root = new GameObject("PUR_NativeCards", new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppSystem.Type>(new[] { Il2CppInterop.Runtime.Il2CppType.Of<RectTransform>() }));
        Root.transform.SetParent(panel,false);
        var rootRect = Root.GetComponent<RectTransform>();
        rootRect.anchorMin=Vector2.zero; rootRect.anchorMax=Vector2.one; rootRect.offsetMin=Vector2.zero; rootRect.offsetMax=Vector2.zero;
        var originals=manager.nodes.ToArray();
        var sourceIds=new[]{SkillId.StartingCashI,SkillId.BruteForceI,SkillId.ProfitBoostI,SkillId.GrinderSpeed,SkillId.NeverSkipLegs,SkillId.NeverSkipLegs,SkillId.ShinyShipment,SkillId.LuxuryCans,SkillId.GrinderSpeed,SkillId.GrinderSpeed,SkillId.GrinderSpeed,SkillId.LuckyHits,SkillId.NeverSkipLegs,SkillId.ShinyShipment,SkillId.DamagedDelivery,SkillId.StartingShockwave,SkillId.FirstBlood,SkillId.DeathTouch,SkillId.BruteForceI};
        var panelSize=rootRect.rect.size;
        for(var col=0;col<4;col++) {
            Panel(Root.transform,"PUR_Column_"+col,new Vector2(.14f+col*.24f,.48f),new Vector2(panelSize.x*.23f,panelSize.y*.66f),PanelGreen);
            Panel(Root.transform,"PUR_ColumnAccent_"+col,new Vector2(.14f+col*.24f,.807f),new Vector2(panelSize.x*.23f,2),Orange);
        }
        for(var i=0;i<Balance.Upgrades.Length;i++) {
            var def=Balance.Upgrades[i];var col=def.Group;var row=Array.IndexOf(Balance.Columns[col],i);
            var template=originals.FirstOrDefault(n=>n!=null&&n.skill!=null&&n.skill.skillId==sourceIds[i])??originals[0];
            var card=Clone(template,Root.transform,"PUR_Permanent_"+i,i);
            var size=new Vector2(panelSize.x*.208f,panelSize.y*.083f);
            Place(card.GetComponent<RectTransform>(),new Vector2(.14f+col*.24f,.70f-row*.095f),size);
            FillBackground(card);card.background.sprite=null;card.background.color=RowGreen;
            Place(card.icon.rectTransform,new Vector2(.09f,.59f),new Vector2(size.y*.38f,size.y*.38f));
            Place(card.titleText.rectTransform,new Vector2(.575f,.78f),new Vector2(size.x*.77f,size.y*.27f));
            card.titleText.fontSize=12;card.titleText.enableAutoSizing=true;card.titleText.fontSizeMin=9;card.titleText.fontSizeMax=12;
            card.titleText.alignment=TextAlignmentOptions.MidlineLeft;card.titleText.margin=Vector4.zero;
            Place(card.priceButton.rectTransform,new Vector2(.835f,.34f),new Vector2(size.x*.24f,size.y*.36f));
            card.priceText.transform.SetParent(card.priceButton.transform,false);
            Place(card.priceText.rectTransform,new Vector2(.5f,.5f),new Vector2(size.x*.21f,size.y*.31f));
            card.priceText.alignment=TextAlignmentOptions.Center;card.priceText.enableAutoSizing=true;card.priceText.fontSizeMin=8;card.priceText.fontSizeMax=12;
            foreach(var image in card.priceButton.GetComponentsInChildren<Image>(true))if(image!=card.priceButton)image.gameObject.SetActive(false);
            var rankLabel=Object.Instantiate(card.titleText,card.transform);rankLabel.name="PUR_RankLabel";rankLabel.raycastTarget=false;
            rankLabel.fontSizeMin=8;rankLabel.fontSizeMax=10;rankLabel.alignment=TextAlignmentOptions.MidlineLeft;
            Place(rankLabel.rectTransform,new Vector2(.44f,.47f),new Vector2(size.x*.50f,size.y*.22f));
            var bonus=Object.Instantiate(rankLabel,card.transform);bonus.name="PUR_Bonus";bonus.fontSizeMax=10;bonus.fontSizeMin=8;
            Place(bonus.rectTransform,new Vector2(.44f,.21f),new Vector2(size.x*.50f,size.y*.24f));
            rankLabel.color=new Color(.65f,.76f,.69f,1);bonus.color=Cream;
            var track=Panel(card.transform,"PUR_ProgressTrack",new Vector2(.5f,.035f),new Vector2(size.x*.90f,2),new Color(.22f,.34f,.29f,1));
            var progress=Panel(track.transform,"PUR_Progress",new Vector2(.5f,.5f),new Vector2(0,2),Orange);
            progress.rectTransform.pivot=new Vector2(0,.5f);progress.rectTransform.anchorMin=progress.rectTransform.anchorMax=new Vector2(0,.5f);progress.rectTransform.anchoredPosition=Vector2.zero;
            Progress.Add(progress);
            Cards.Add(card);Bonuses.Add(bonus);RankLabels.Add(rankLabel);
            var iconId=i==4?214:i is 9 or 10?214:i==8?155:i==12?192:i==18?46:-1;
            if(iconId>=0&&manager.skillTree!=null){var icon=manager.skillTree.GetComponentsInChildren<SkillNode>(true).FirstOrDefault(n=>n.skill!=null&&(int)n.skill.skillId==iconId);if(icon!=null)card.icon.sprite=icon.icon.sprite;}
        }
        GroupHeaders.Clear();
        for(var col=0;col<4;col++) {
            var heading=Object.Instantiate(Cards[0].titleText,Root.transform);heading.text=Balance.Groups[col];heading.fontSizeMin=12;heading.fontSizeMax=18;heading.alignment=TextAlignmentOptions.Center;heading.color=Cream;heading.raycastTarget=false;
            Place(heading.rectTransform,new Vector2(.14f+col*.24f,.77f),new Vector2(panelSize.x*.22f,panelSize.y*.045f));GroupHeaders.Add(heading);
        }
        Panel(Root.transform,"PUR_DescriptionPanel",new Vector2(.5f,.112f),new Vector2(panelSize.x*.95f,panelSize.y*.065f),PanelGreen);
        Tooltip=Object.Instantiate(Cards[0].titleText,Root.transform);Tooltip.text="Survolez une amélioration pour afficher ses détails.";Tooltip.alignment=TextAlignmentOptions.Center;Tooltip.fontSizeMin=9;Tooltip.fontSizeMax=12;Tooltip.overflowMode=TextOverflowModes.Truncate;Tooltip.raycastTarget=false;Tooltip.color=Cream;Tooltip.enableWordWrapping=true;
        Place(Tooltip.rectTransform,new Vector2(.5f,.115f),new Vector2(panelSize.x*.90f,panelSize.y*.055f));
        // Hide the original graph's container, retaining its objects for price/refund lookup.
        foreach(var node in originals) if(node != null) {
            node.gameObject.SetActive(false);
            var parent=node.transform.parent;
            if(parent != null && parent!=panel && !Root.transform.IsChildOf(parent)) parent.gameObject.SetActive(false);
        }
        foreach(var image in panel.GetComponentsInChildren<Image>(true)) {
            if(image.transform.IsChildOf(Root.transform))continue;
            var rect=image.rectTransform.rect;
            var name=image.name.ToLowerInvariant();
            if(name.StartsWith("line") || name.Contains("connection") || (Math.Min(rect.width,rect.height)<4 && Math.Max(rect.width,rect.height)>30))image.gameObject.SetActive(false);
        }
        var headerImages=manager.pointsText.transform.parent.GetComponentsInChildren<Image>(true).Where(im=>im.sprite!=null).ToArray();
        var creamIcon=headerImages.FirstOrDefault(im=>im.name.ToLowerInvariant().Contains("icon")) ?? headerImages.OrderBy(im=>im.rectTransform.rect.width).FirstOrDefault();
        var canvas=manager.permanentUpgradePanel.GetComponentInParent<Canvas>();
        Access=Clone(originals[0],canvas.transform,"PUR_Open",100);
        if(creamIcon!=null) Access.icon.sprite=creamIcon.sprite;
        Access.titleText.text="PERMANENTS · P"; Access.titleText.fontSize=11; Access.titleText.enableAutoSizing=true;
        Access.priceButton.gameObject.SetActive(false);
        Place(Access.GetComponent<RectTransform>(),new Vector2(1,1),new Vector2(125,38),new Vector2(-86,-100));
        FillBackground(Access);
        Place(Access.icon.rectTransform,new Vector2(.13f,.5f),new Vector2(22,22));
        Place(Access.titleText.rectTransform,new Vector2(.62f,.5f),new Vector2(91,26));
        AddWholeCardButton(Access,()=>Toggle());
        BulkBuy=Clone(originals[0],manager.skillTree.canvas.transform,"PUR_BulkBuy",110);
        BulkBuy.icon.gameObject.SetActive(false);BulkBuy.priceButton.gameObject.SetActive(false);
        Place(BulkBuy.GetComponent<RectTransform>(),new Vector2(0,1),new Vector2(285,44),new Vector2(170,-100));FillBackground(BulkBuy);
        Place(BulkBuy.titleText.rectTransform,new Vector2(.5f,.5f),new Vector2(265,38));BulkBuy.titleText.fontSizeMin=9;BulkBuy.titleText.fontSizeMax=12;BulkBuy.titleText.enableAutoSizing=true;BulkBuy.titleText.alignment=TextAlignmentOptions.Center;
        AddWholeCardButton(BulkBuy,()=>{BulkPurchase.Buy();Refresh();});
        Close=Clone(originals[0],Root.transform,"PUR_Close",101);
        Close.icon.gameObject.SetActive(false); Close.priceButton.gameObject.SetActive(false);
        Close.titleText.text="FERMER [P]"; Close.titleText.fontSize=14;Close.titleText.alignment=TextAlignmentOptions.Center;
        Place(Close.titleText.rectTransform,new Vector2(.5f,.5f),new Vector2(145,30));
        Place(Close.GetComponent<RectTransform>(),new Vector2(.83f,.12f),new Vector2(160,40));
        FillBackground(Close);
        AddWholeCardButton(Close,()=>Toggle());
        Refund=Clone(originals[0],Root.transform,"PUR_Refund",102);
        Refund.icon.gameObject.SetActive(false); Refund.priceButton.gameObject.SetActive(false);
        Refund.titleText.fontSize=12; Refund.titleText.enableAutoSizing=true;Refund.titleText.fontSizeMin=9;Refund.titleText.fontSizeMax=12;Refund.titleText.alignment=TextAlignmentOptions.Center;
        Place(Refund.titleText.rectTransform,new Vector2(.5f,.5f),new Vector2(270,30));
        Place(Refund.GetComponent<RectTransform>(),new Vector2(.26f,.12f),new Vector2(290,40));
        FillBackground(Refund);
        AddWholeCardButton(Refund,()=>RefundClick());
        Place(Close.GetComponent<RectTransform>(),new Vector2(.85f,.055f),new Vector2(160,32));
        Place(Refund.GetComponent<RectTransform>(),new Vector2(.24f,.055f),new Vector2(290,32));
        Root.transform.SetAsLastSibling();Access.transform.SetAsLastSibling();
        Mod.Self.LoggerInstance.Msg("Native UI built: 19 permanent rows, padded dark columns, visible icons/progress/description; batch shop controls added.");
        Refresh();
    }

    static Image Panel(Transform parent,string name,Vector2 anchor,Vector2 size,Color color)
    {
        var go=new GameObject(name,new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Il2CppSystem.Type>(new[]{Il2CppInterop.Runtime.Il2CppType.Of<RectTransform>()}));go.transform.SetParent(parent,false);
        var image=go.AddComponent<Image>();image.color=color;image.raycastTarget=false;Place(image.rectTransform,anchor,size);return image;
    }
    static void Place(RectTransform rt,Vector2 anchor,Vector2 size,Vector2 offset=default)
    {
        rt.anchorMin=rt.anchorMax=anchor;rt.pivot=new Vector2(.5f,.5f);rt.sizeDelta=size;rt.anchoredPosition=offset;rt.localScale=Vector3.one;rt.localRotation=Quaternion.identity;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=Vector2.zero;rt.offsetMax=Vector2.zero;rt.localScale=Vector3.one;
    }

    static void FillBackground(PermanentUpgrade card)
    {
        if(card.background.transform!=card.transform)Stretch(card.background.rectTransform);
    }

    static PermanentUpgrade Clone(PermanentUpgrade template,Transform parent,string name,int action)
    {
        var clone=Object.Instantiate(template,parent);
        clone.name=name; Actions[clone.Pointer]=action;clone.gameObject.SetActive(true);
        clone.icon.transform.SetParent(clone.transform,false);
        clone.titleText.transform.SetParent(clone.transform,false);
        clone.titleText.margin=Vector4.zero;
        clone.titleText.overflowMode=TextOverflowModes.Ellipsis;
        foreach(var translation in clone.GetComponentsInChildren<TranslateText>(true))translation.enabled=false;
        // Use the game's own unscaled hover/click animation and UI sounds.
        foreach(var animation in clone.GetComponentsInChildren<ButtonAnimation>(true))animation.enabled=true;
        if(clone.GetComponentsInChildren<ButtonAnimation>(true).Length==0)clone.gameObject.AddComponent<ButtonAnimation>();
        foreach(var layout in clone.GetComponentsInChildren<ContentSizeFitter>(true))layout.enabled=false;
        if(clone.priceSeparator!=null)clone.priceSeparator.SetActive(false);
        clone.background.sprite=template.availableSprite;clone.background.color=Color.white;
        clone.titleText.color=Cream;clone.icon.color=Cream;clone.priceText.color=new Color(.10f,.18f,.16f,1);
        // Replace cloned UnityEvents, including persistent listeners, with only our purchase action.
        foreach(var button in clone.GetComponentsInChildren<Button>(true)) {
            ConfigureFeedback(button);
            button.onClick=new Button.ButtonClickedEvent();
            var index=action;
            button.onClick.AddListener((UnityAction)(()=>Run(index)));
        }
        return clone;
    }

    static void AddWholeCardButton(PermanentUpgrade card,Action action)
    {
        var button=card.gameObject.GetComponent<Button>() ?? card.gameObject.AddComponent<Button>();
        button.targetGraphic=card.background;button.onClick=new Button.ButtonClickedEvent();
        ConfigureFeedback(button);
        var animation=card.gameObject.GetComponent<ButtonAnimation>() ?? card.gameObject.AddComponent<ButtonAnimation>();
        animation.enabled=true;
        button.onClick.AddListener((UnityAction)(()=>action()));
        card.background.raycastTarget=true;
    }

    static void ConfigureFeedback(Button button)
    {
        button.transition=Selectable.Transition.ColorTint;
        var colors=button.colors;
        colors.normalColor=Color.white;
        colors.highlightedColor=new Color(1.12f,1.12f,1.04f,1);
        colors.selectedColor=Color.white;
        colors.pressedColor=new Color(.92f,.83f,.64f,1);
        colors.disabledColor=new Color(.65f,.65f,.65f,1);
        colors.colorMultiplier=1;colors.fadeDuration=.10f;
        button.colors=colors;
    }

    internal static void Refresh()
    {
        if(Manager==null || Root==null)return;
        if(Manager.permanentUpgradePanel.gameObject.activeInHierarchy&&Manager.permanentUpgradePanel.alpha>.05f)for(var i=0;i<Cards.Count;i++) {
            var rank=Mod.Ranks[i];var upgrade=Balance.Upgrades[i];var max=rank>=upgrade.Cap;
            var progressSize=Progress[i].rectTransform.sizeDelta;progressSize.x=Cards[i].GetComponent<RectTransform>().rect.width*.90f*rank/upgrade.Cap;Progress[i].rectTransform.sizeDelta=progressSize;
            Cards[i].gameObject.SetActive(true);Cards[i].titleText.text=upgrade.Name;
            var chance=Specials.FinalChance(i);
            RankLabels[i].text=$"{rank}/{upgrade.Cap}"+(chance.Length>0?" · "+chance:"");
            Bonuses[i].text=upgrade.Unit=="unlock"?(max?"ACQUIS":"ACHAT UNIQUE"):Balance.Display(i,rank)+(max?" · MAX":" → "+Balance.Display(i,rank+1));
            var dependency=Balance.CanPurchase(i,Mod.Ranks);
            Cards[i].priceText.text=max?"MAX":!dependency?"PORTÉE I":Balance.Price(i,rank).ToString("N0")+" P";
            var affordable=Mod.CanShop()&&!max&&dependency&&SaveManager.Instance.CurrentSaveData.upgradePoints>=Balance.Price(i,rank);
            Cards[i].priceButton.color=affordable?Orange:new Color(.5f,.5f,.40f,1);
            foreach(var button in Cards[i].GetComponentsInChildren<Button>(true))button.interactable=affordable;
        }
        Refund.titleText.text=ConfirmRefund?"CONFIRMER LE REMBOURSEMENT":"REDISTRIBUER MES POINTS";
        Close.titleText.text="FERMER [P]";Access.titleText.text="PERMANENTS · P";
        var bulkVisible=CanBulkShop();BulkBuy.gameObject.SetActive(bulkVisible);
        if(LastBulkVisible!=(bulkVisible?1:0)){LastBulkVisible=bulkVisible?1:0;Mod.Self.LoggerInstance.Msg("Bulk controls: "+(bulkVisible?"visible in native shop":"hidden outside native shop"));}
        if(bulkVisible) {
            var preview=BulkPurchase.Preview();var enough=MoneyManager.Instance!=null&&MoneyManager.Instance.Money>=preview.Cost;
            BulkBuy.titleText.text=preview.Ids.Length==0?"AUCUN ACHAT POSSIBLE":$"ACHETER AU MAXIMUM\n{preview.Ids.Length} achats · {preview.Cost:N0} $";
            if(!enough)BulkBuy.titleText.text+=" · FONDS INSUFFISANTS";
            BulkBuy.GetComponent<Button>().interactable=preview.Ids.Length>0&&enough&&!BulkPurchase.Running;
        }
        Close.gameObject.SetActive(Opened);
        Access.gameObject.SetActive(!Opened && IsShop() && (!Manager.permanentUpgradePanel.gameObject.activeInHierarchy || Manager.permanentUpgradePanel.alpha<.05f));
    }

    internal static bool CanBulkShop()=>!Opened&&IsShop()&&GameManager.Instance.skillTreePanel.panel.activeInHierarchy;

    static bool IsShop()
    {
        var gm=GameManager.Instance;
        if(gm==null||gm.skillTreeView==null||gm.skillTreeView.canvas==null||!gm.skillTreeView.canvas.isActiveAndEnabled)return false;
        if(Cursor.lockState!=CursorLockMode.None||!Cursor.visible)return false;
        if(gm.pauseMenu!=null&&gm.pauseMenu.gameObject.activeInHierarchy&&gm.pauseMenu.alpha>.05f)return false;
        if(Menu.Instance!=null&&Menu.Instance.menuPanel!=null&&Menu.Instance.menuPanel.activeInHierarchy)return false;
        if(gm.skillTreePanel==null||!gm.skillTreePanel.gameObject.activeInHierarchy)return false;
        foreach(var canvas in gm.skillTreeView.GetComponentsInParent<Canvas>(true))if(!canvas.isActiveAndEnabled)return false;
        if(gm.skillTreeView.GetComponentsInParent<CanvasGroup>(true).Any(group=>group.alpha<.95f||!group.interactable||!group.blocksRaycasts))return false;
        return new[]{gm.skillTreePanel,gm.mailPanel,gm.gamblingPanel,gm.sellPanel}.Any(tab=>tab!=null && tab.panel!=null && tab.panel.activeInHierarchy);
    }

    internal static void Click(PermanentUpgrade node) { if(Actions.TryGetValue(node.Pointer,out var action))Run(action); }
    static void Run(int action) {if(action>=0&&action<Balance.Upgrades.Length){Mod.Purchase(action);Refresh();}else if(action==100||action==101)Toggle();else if(action==102)RefundClick();else if(action==110){BulkPurchase.Buy();Refresh();}}
    static void Hover(PermanentUpgrade card,bool enter) {
        if(Tooltip==null)return;
        if(enter&&Actions.TryGetValue(card.Pointer,out var index)&&index>=0&&index<Balance.Upgrades.Length)Tooltip.text=Balance.Upgrades[index].Description;
        else Tooltip.text="Survolez une amélioration pour afficher ses détails.";
    }
    static void RefundClick() { if(!Mod.CanShop())return; if(ConfirmRefund) {Mod.Respec();ConfirmRefund=false;}else ConfirmRefund=true; Refresh(); }

    internal static void ReadInput()
    {
        if(Manager==null)return;
        if((Opened||IsShop()) && Input.GetKeyDown(KeyCode.P))Toggle();
        else if(Opened && Input.GetKeyDown(KeyCode.Escape))Toggle();
    }

    static void Toggle()
    {
        if(Manager==null || !Mod.CanShop())return;
        var panel=Manager.permanentUpgradePanel;
        var gm=GameManager.Instance;
        if(!Opened) {
            if(!IsShop())return;
            // The native post-sale screen already owns this panel; leave NEXT DAY intact there.
            if(panel.gameObject.activeInHierarchy && panel.alpha>.5f)return;
            PreviousActive=panel.gameObject.activeSelf;PreviousAlpha=panel.alpha;PreviousTime=Time.timeScale;
            PreviousPaused=gm!=null&&gm.IsPaused;PreviousCursor=Cursor.lockState;PreviousCursorVisible=Cursor.visible;
            TemporarilyHidden.Clear();
            foreach(var button in panel.GetComponentsInChildren<Button>(true)) {
                if(button.transform.IsChildOf(Root.transform))continue;
                if(button.gameObject.activeSelf) {TemporarilyHidden.Add(button.gameObject);button.gameObject.SetActive(false);}
            }
            Opened=true;panel.gameObject.SetActive(true);panel.alpha=1;panel.interactable=true;panel.blocksRaycasts=true;
            panel.transform.SetAsLastSibling(); if(gm!=null)gm.IsPaused=true;
            Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        } else {
            Opened=false;panel.alpha=PreviousAlpha;panel.gameObject.SetActive(PreviousActive);
            foreach(var obj in TemporarilyHidden)if(obj!=null)obj.SetActive(true);
            TemporarilyHidden.Clear();if(gm!=null)gm.IsPaused=PreviousPaused;
            Time.timeScale=PreviousTime;Cursor.lockState=PreviousCursor;Cursor.visible=PreviousCursorVisible;ConfirmRefund=false;
        }
        Refresh();
    }

    [HarmonyPatch(typeof(PermanentUpgrade),"Update")]
    static class CardUpdate { static bool Prefix(PermanentUpgrade __instance)=>!Actions.ContainsKey(__instance.Pointer); }
    [HarmonyPatch(typeof(PermanentUpgrade),"OnPointerEnter")]
    static class CardHover { static bool Prefix(PermanentUpgrade __instance){if(!Actions.ContainsKey(__instance.Pointer))return true;Hover(__instance,true);return false;} }
    [HarmonyPatch(typeof(PermanentUpgrade),"OnPointerExit")]
    static class CardExit {static bool Prefix(PermanentUpgrade __instance){if(!Actions.ContainsKey(__instance.Pointer))return true;Hover(__instance,false);return false;}}
    [HarmonyPatch(typeof(PermanentUpgrade),"ApplyInitialState")]
    static class CardInitial { static bool Prefix(PermanentUpgrade __instance)=>!Actions.ContainsKey(__instance.Pointer); }
    [HarmonyPatch(typeof(GameManager),"UpdatePause")]
    static class PausePatch { static bool Prefix()=>!Opened; }
    [HarmonyPatch(typeof(GameManager),"UpdatePauseInput")]
    static class PauseInputPatch { static bool Prefix()=>!Opened; }
}
