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
        Player = player;
        state = inventory ?? new MoonBaseState();
        state.battery = Mathf.Clamp(state.battery, 0, 100);
        state.water = Mathf.Max(0, state.water);
        state.food = Mathf.Max(0, state.food);
        state.medicine = Mathf.Max(0, state.medicine);
        if (state.openedCrates == null || state.openedCrates.Length != 5)
            state.openedCrates = new bool[5];
        if (Player != null)
            controller = (CharacterController)Player.GetComponent(typeof(CharacterController));
        ResolveStats();
    }

    void ResolveStats()
    {
        if (needs == null)
            needs = Player != null ? (BarsLogic)Player.GetComponentInChildren(typeof(BarsLogic)) : null;
        if (needs == null)
            needs = FindAnyObjectByType<BarsLogic>();
        if (health == null)
            health = Player != null ? (BarsHpLogic)Player.GetComponentInChildren(typeof(BarsHpLogic)) : null;
        if (health == null)
            health = StaticBarsLogic.model;
    }

    public void Register(BaseStation station)
    {
        if (!stations.Contains(station))
            stations.Add(station);
    }

    public void RegisterLamp(Light lamp)
    {
        lamps.Add(lamp);
        lamp.enabled = Powered;
    }

    void Update()
    {
        if (state == null) return;
        AdvancePower(Time.deltaTime);
        if (Player == null) return;
        Keyboard keys = Keyboard.current;
        if (!Application.isFocused || keys == null || Time.timeScale <= 0)
        {
            progress = 0;
            return;
        }
        if (keys.escapeKey.wasPressedThisFrame)
            statusOpen = false;
        BaseStation closest = null;
        float best = float.PositiveInfinity;
        foreach (var station in stations)
        {
            if (station == null || !station.isActiveAndEnabled) continue;
            float d = (station.transform.position - Player.position).sqrMagnitude;
            if (d < best && CanReach(station))
            {
                best = d;
                closest = station;
            }
        }
        if (closest != selected)
        {
            progress = 0;
            selected = closest;
        }
        bool held = keys.fKey.isPressed;
        if (!held)
        {
            waitForRelease = false;
            progress = 0;
            return;
        }
        if (selected == null || waitForRelease) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            progress = 0;
            return;
        }
        if (controller != null && (!controller.isGrounded || Vector3.ProjectOnPlane(controller.velocity, Vector3.up).sqrMagnitude > .04f))
        {
            progress = 0;
            return;
        }
        string blocked = Unavailable(selected);
        if (blocked != null)
        {
            if (keys.fKey.wasPressedThisFrame)
                Show(blocked);
            progress = 0;
            return;
        }
        progress += Time.deltaTime;
        if (progress >= selected.holdSeconds)
        {
            TryUse(selected, out string result);
            Show(result);
            progress = 0;
            waitForRelease = true;
        }
    }

    void AdvancePower(float dt)
    {
        if (state == null) return;
        float change = (state.solarOn ? .30f : 0) - (state.powerOn ? .12f : 0);
        state.battery = Mathf.Clamp(state.battery + change * Mathf.Max(0, dt), 0, 100);
        if (state.battery <= 0)
            state.powerOn = false;
        foreach (var lamp in lamps)
            if (lamp != null)
                lamp.enabled = Powered;
        foreach (var station in stations)
            if (station != null && station.usedMarker != null && station.supplyIndex >= 0 && station.supplyIndex < 5)
                station.usedMarker.SetActive(state.openedCrates[station.supplyIndex]);
    }

    public bool CanReach(BaseStation station)
    {
        if (Player == null || station == null || station.Owner != this || !station.isActiveAndEnabled) return false;
        Vector3 origin = controller != null ? controller.bounds.center : Player.position + Vector3.up * .8f;
        Vector3 delta = station.transform.position - origin;
        float distance = delta.magnitude;
        if (distance > 2.5f) return false;
        if (controller != null && !controller.isGrounded) return false;
        if (distance < .05f) return true;
        int count = Physics.RaycastNonAlloc(origin, delta / distance, hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Transform t = hits[i].transform;
            if (t == null || t == Player || t.IsChildOf(Player) || t == station.transform || t.IsChildOf(station.transform)) continue;
            if (hits[i].distance < distance - .05f) return false;
        }
        return true;
    }

    string Unavailable(BaseStation station)
    {
        if (state == null) return "Base is not ready yet";
        ResolveStats();
        switch (station.kind)
        {
            case BaseStation.UseKind.Water:
                if (needs == null) return "Player stats unavailable";
                if (needs.ThirstPercent >= 99.9f) return "Water not needed";
                if (state.water <= 0) return "Out of water — check supply containers";
                if (!Powered || state.battery < 1) return "Dispenser requires power";
                break;
            case BaseStation.UseKind.Food:
                if (needs == null) return "Player stats unavailable";
                if (needs.HungerPercent >= 99.9f) return "Not hungry";
                if (state.food <= 0) return "Out of food — check supply containers";
                break;
            case BaseStation.UseKind.Rest:
                if (needs == null) return "Player stats unavailable";
                if (needs.MindPercent >= 99.9f) return "Rest not needed yet";
                break;
            case BaseStation.UseKind.Workout:
                if (needs == null) return "Player stats unavailable";
                if (needs.MindPercent >= 99.9f) return "Sanity is already full";
                break;
            case BaseStation.UseKind.Medical:
                if (health == null) return "Health status unavailable";
                if (!health.HasInjuries) return "No injuries to treat";
                if (state.medicine <= 0) return "Out of medical supplies";
                if (!Powered || state.battery < 4) return "Medical station requires power";
                break;
            case BaseStation.UseKind.Power:
                if (!state.powerOn && state.battery < 1) return "Charge battery via solar panels first";
                break;
            case BaseStation.UseKind.Supply:
                if (station.supplyIndex < 0 || station.supplyIndex >= 5 || state.openedCrates[station.supplyIndex]) return "Container is already empty";
                break;
        }
        return null;
    }

    public bool TryUse(BaseStation station, out string message)
    {
        if (!CanReach(station))
        {
            message = "Move closer to the station on flat ground";
            return false;
        }
        string blocked = Unavailable(station);
        if (blocked != null)
        {
            message = blocked;
            return false;
        }
        switch (station.kind)
        {
            case BaseStation.UseKind.Water:
                needs.ChangeThirst(40);
                state.water--;
                state.battery -= 1;
                message = "Water +40";
                break;
            case BaseStation.UseKind.Food:
                needs.ChangeHunger(35);
                state.food--;
                message = "Hunger +35";
                break;
            case BaseStation.UseKind.Rest:
                needs.ChangeMind(40);
                message = "Sanity restored: +40";
                break;
            case BaseStation.UseKind.Workout:
                needs.ChangeMind(35);
                message = "Workout complete! Sanity +35";
                break;
            case BaseStation.UseKind.Medical:
                health.RestoreBody();
                state.medicine--;
                state.battery -= 4;
                message = "Injuries treated";
                break;
            case BaseStation.UseKind.Command:
                statusOpen = !statusOpen;
                message = statusOpen ? "Base status opened" : "Base status closed";
                break;
            case BaseStation.UseKind.Power:
                state.powerOn = !state.powerOn;
                message = state.powerOn ? "Base power ON" : "Base power OFF";
                break;
            case BaseStation.UseKind.Solar:
                state.solarOn = !state.solarOn;
                message = state.solarOn ? "Solar charging ON" : "Solar charging OFF";
                break;
            case BaseStation.UseKind.Supply:
                state.openedCrates[station.supplyIndex] = true;
                state.water += 4;
                state.food += 4;
                state.medicine += 2;
                message = "Supplies collected: +4 Water, +4 Food, +2 Meds";
                break;
            default:
                message = "Done";
                break;
        }
        return true;
    }

    void Show(string text)
    {
        feedback = text;
        feedbackUntil = Time.unscaledTime + 3f;
    }

    void OnGUI()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
        }
        if (selected != null)
        {
            string prompt = $"[F] {selected.stationName}" + (progress > 0.01f ? $" ({Mathf.Clamp01(progress / selected.holdSeconds) * 100f:0}%)" : "");
            GUI.Box(new Rect(Screen.width * 0.5f - 170f, Screen.height - 95f, 340f, 36f), prompt, labelStyle);
        }
        if (Time.unscaledTime < feedbackUntil && !string.IsNullOrEmpty(feedback))
        {
            GUI.Box(new Rect(Screen.width * 0.5f - 190f, Screen.height - 140f, 380f, 36f), feedback, labelStyle);
        }
        if (statusOpen && state != null)
        {
            string status = $"BASE STATUS (ESC to close)\nPower: {(Powered ? "ONLINE" : "OFFLINE")} | Battery: {state.battery:0}%\nSolar Array: {(state.solarOn ? "CHARGING" : "OFF")}\nWater: {state.water} | Food: {state.food} | Meds: {state.medicine}";
            GUI.Box(new Rect(Screen.width * 0.5f - 180f, 80f, 360f, 110f), status, labelStyle);
        }
    }
}
