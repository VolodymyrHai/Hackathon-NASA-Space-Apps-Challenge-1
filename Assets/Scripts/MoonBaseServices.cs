using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[Serializable]
public class MoonBaseState
{
    public int water = 12, food = 12, medicine = 6;
    public float battery = 100;
    public bool powerOn = true, solarOn = true;
    public bool[] openedCrates = new bool[5];
}

public class MoonBaseServices : MonoBehaviour
{
    public Transform Player { get; private set; }
    public bool Powered => state != null && state.powerOn && state.battery > 0;
    public MoonBaseState State => state;
    readonly List<BaseStation> stations = new List<BaseStation>();
    readonly List<Light> lamps = new List<Light>();
    readonly RaycastHit[] hits = new RaycastHit[48];
    MoonBaseState state;
    BarsLogic needs;
    BarsHpLogic health;
    CharacterController controller;
    BaseStation selected;
    float progress, feedbackUntil;
    string feedback = "";
    bool waitForRelease, statusOpen;
    GUIStyle labelStyle;

    public void Initialize(Transform player, MoonBaseState inventory)
    {
        Player=player; state=inventory??new MoonBaseState();
        state.battery=Mathf.Clamp(state.battery,0,100);
        state.water=Mathf.Max(0,state.water);state.food=Mathf.Max(0,state.food);state.medicine=Mathf.Max(0,state.medicine);
        if(state.openedCrates==null||state.openedCrates.Length!=5)state.openedCrates=new bool[5];
        if(Player!=null)controller=Player.GetComponent<CharacterController>();
        ResolveStats();
    }
    void ResolveStats()
    {
        if(needs==null)needs=Player!=null?Player.GetComponentInChildren<BarsLogic>():null;
        if(needs==null)needs=FindAnyObjectByType<BarsLogic>();
        if(health==null)health=Player!=null?Player.GetComponentInChildren<BarsHpLogic>():null;
        if(health==null)health=StaticBarsLogic.model;
    }
    public void Register(BaseStation station) { if(!stations.Contains(station))stations.Add(station); }
    public void RegisterLamp(Light lamp) { lamps.Add(lamp);lamp.enabled=Powered; }
    void Update()
    {
        if(state==null)return;
        AdvancePower(Time.deltaTime);
        if(Player==null)return;
        Keyboard keys=Keyboard.current;
        if(!Application.isFocused||keys==null||Time.timeScale<=0){progress=0;return;}
        if(keys.escapeKey.wasPressedThisFrame)statusOpen=false;
        BaseStation closest=null;float best=float.PositiveInfinity;
        foreach(var station in stations)
        {
            if(station==null||!station.isActiveAndEnabled)continue;
            float d=(station.transform.position-Player.position).sqrMagnitude;
            if(d<best&&CanReach(station)){best=d;closest=station;}
        }
        if(closest!=selected){progress=0;selected=closest;}
        bool held=keys.fKey.isPressed;
        if(!held){waitForRelease=false;progress=0;return;}
        if(selected==null||waitForRelease)return;
        if(EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject()){progress=0;return;}
        if(controller!=null&&(!controller.isGrounded||Vector3.ProjectOnPlane(controller.velocity,Vector3.up).sqrMagnitude>.04f)){progress=0;return;}
        string blocked=Unavailable(selected);
        if(blocked!=null){if(keys.fKey.wasPressedThisFrame)Show(blocked);progress=0;return;}
        progress+=Time.deltaTime;
        if(progress>=selected.holdSeconds)
        {
            TryUse(selected,out string result);Show(result);progress=0;waitForRelease=true;
        }
    }
    void AdvancePower(float dt)
    {
        if(state==null)return;
        float change=(state.solarOn?.30f:0)-(state.powerOn?.12f:0);
        state.battery=Mathf.Clamp(state.battery+change*Mathf.Max(0,dt),0,100);
        if(state.battery<=0)state.powerOn=false;
        foreach(var lamp in lamps)if(lamp!=null)lamp.enabled=Powered;
        foreach(var station in stations)
            if(station!=null&&station.usedMarker!=null&&station.supplyIndex>=0&&station.supplyIndex<5)
                station.usedMarker.SetActive(state.openedCrates[station.supplyIndex]);
    }
    public bool CanReach(BaseStation station)
    {
        if(Player==null||station==null||station.Owner!=this||!station.isActiveAndEnabled)return false;
        Vector3 origin=controller!=null?controller.bounds.center:Player.position+Vector3.up*.8f;
        Vector3 delta=station.transform.position-origin;float distance=delta.magnitude;
        if(distance>2.5f)return false;
        if(controller!=null&&!controller.isGrounded)return false;
        if(distance<.05f)return true;
        int count=Physics.RaycastNonAlloc(origin,delta/distance,hits,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            Transform t=hits[i].transform;
            if(t==null||t==Player||t.IsChildOf(Player)||t==station.transform||t.IsChildOf(station.transform))continue;
            if(hits[i].distance<distance-.05f)return false;
        }
        return true;
    }
    string Unavailable(BaseStation station)
    {
        if(state==null)return "База ещё не готова";
        ResolveStats();
        switch(station.kind)
        {
            case BaseStation.UseKind.Water:
                if(needs==null)return "Показатели игрока недоступны";
                if(needs.ThirstPercent>=99.9f)return "Вода не требуется";
                if(state.water<=0)return "Вода закончилась — проверьте резервные контейнеры";
                if(!Powered||state.battery<1)return "Диспенсеру требуется питание";break;
            case BaseStation.UseKind.Food:
                if(needs==null)return "Показатели игрока недоступны";
                if(needs.HungerPercent>=99.9f)return "Персонаж сыт";
                if(state.food<=0)return "Еда закончилась — проверьте резервные контейнеры";break;
            case BaseStation.UseKind.Rest:
                if(needs==null)return "Показатели игрока недоступны";
                if(needs.MindPercent>=99.9f)return "Отдых пока не требуется";break;
            case BaseStation.UseKind.Medical:
                if(health==null)return "Состояние здоровья недоступно";
                if(!health.HasInjuries)return "Лечение не требуется";
                if(state.medicine<=0)return "Медикаменты закончились";
                if(!Powered||state.battery<4)return "Медстанции требуется питание";break;
            case BaseStation.UseKind.Power:
                if(!state.powerOn&&state.battery<1)return "Сначала зарядите батарею солнечными панелями";break;
            case BaseStation.UseKind.Supply:
                if(station.supplyIndex<0||station.supplyIndex>=5||state.openedCrates[station.supplyIndex])return "Контейнер уже пуст";break;
        }
        return null;
    }
    public bool TryUse(BaseStation station,out string message)
    {
        if(!CanReach(station)){message="Подойдите к станции на земле; проход должен быть свободен";return false;}
        string blocked=Unavailable(station);if(blocked!=null){message=blocked;return false;}
        switch(station.kind)
        {
            case BaseStation.UseKind.Water: needs.ChangeThirst(40);state.water--;state.battery-=1;message="Вода +40";break;
            case BaseStation.UseKind.Food: needs.ChangeHunger(35);state.food--;message="Сытость +35";break;
            case BaseStation.UseKind.Rest: needs.ChangeMind(40);message="Состояние восстановлено: +40";break;
            case BaseStation.UseKind.Medical: health.RestoreBody();state.medicine--;state.battery-=4;message="Здоровье восстановлено, переломы вылечены";break;
            case BaseStation.UseKind.Power: state.powerOn=!state.powerOn;message=state.powerOn?"Питание базы включено":"Питание выключено, двери открыты для эвакуации";break;
            case BaseStation.UseKind.Solar: state.solarOn=!state.solarOn;message=state.solarOn?"Зарядка от панелей включена":"Зарядка от панелей отключена";break;
            case BaseStation.UseKind.Supply: state.openedCrates[station.supplyIndex]=true;state.water+=4;state.food+=4;state.medicine++;message="Резерв: вода +4, еда +4, медикаменты +1";break;
            default: statusOpen=!statusOpen;message="Состояние базы обновлено";break;
        }
        state.battery=Mathf.Max(0,state.battery);AdvancePower(0);return true;
    }
    void Show(string text){feedback=text;feedbackUntil=Time.unscaledTime+3;}
    void OnDisable(){selected=null;progress=0;statusOpen=false;}
    void OnGUI()
    {
        if(!Application.isPlaying||state==null||Player==null)return;
        if(labelStyle==null)labelStyle=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=true,alignment=TextAnchor.MiddleCenter};
        float width=Mathf.Min(620,Screen.width-24);
        string line=Time.unscaledTime<feedbackUntil?feedback:selected!=null?"Удерживайте F — "+selected.stationName:"";
        if(line.Length>0)
        {
            var rect=new Rect((Screen.width-width)*.5f,Screen.height-110,width,75);GUI.Box(rect,GUIContent.none);GUI.Label(new Rect(rect.x+12,rect.y+5,rect.width-24,44),line,labelStyle);
            if(selected!=null&&progress>0)GUI.Box(new Rect(rect.x+12,rect.y+56,(rect.width-24)*Mathf.Clamp01(progress/selected.holdSeconds),8),GUIContent.none);
        }
        if(statusOpen)
        {
            string text=$"ЛУННАЯ БАЗА\nПитание: {(Powered?"включено":"выключено")}\nЗаряд: {state.battery:0}%\nСолнечные панели: {(state.solarOn?"работают":"отключены")}\nВода: {state.water}   Еда: {state.food}\nМедикаменты: {state.medicine}\nEsc — закрыть";
            Rect rect=new Rect(Screen.width-320,24,300,240);GUI.Box(rect,GUIContent.none);GUI.Label(new Rect(rect.x+12,rect.y+8,rect.width-24,rect.height-16),text,labelStyle);
        }
    }
}
