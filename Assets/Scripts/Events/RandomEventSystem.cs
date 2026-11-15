using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RandomEventSystem : MonoBehaviour
{
    [Serializable]
    public class GameEvent
    {
        public string name;

        [Tooltip("Шанс у відсотках (0-100)")]
        [Range(0f, 100f)] public float chance = 5f;

        [Tooltip("Скільки секунд триває подія")]
        [Min(0f)] public float duration = 5f;

        [Tooltip("Скільки секунд після ЗАВЕРШЕННЯ події вона не може повторитися")]
        [Min(0f)] public float cooldown = 0f;

        public UnityEvent onStart; 
        public UnityEvent onEnd;    

        [NonSerialized] public float lastEndTime = -9999f;
    }

    [Header("Налаштування")]
    public float checkInterval = 5f; 
    public bool autoRoll = true;  

    [Header("Список подій")]
    public List<GameEvent> events = new List<GameEvent>();

    public GameEvent ActiveEvent { get; private set; }
    public bool IsEventActive => ActiveEvent != null;

    private float rollTimer;
    private float activeTimer;

    private void Update()
    {
        if (IsEventActive)
        {
            activeTimer -= Time.deltaTime;
            if (activeTimer <= 0f) EndEvent();
            return;
        }

        if (!autoRoll) return;

        rollTimer += Time.deltaTime;
        if (rollTimer >= checkInterval)
        {
            rollTimer = 0f;
            TryTriggerEvent();
        }
    }

    public bool TryTriggerEvent()
    {
        if (IsEventActive) return false;

        float roll = UnityEngine.Random.value * 100f;
        float cumulative = 0f;

        foreach (var e in events)
        {
            if (!IsAvailable(e)) continue;

            cumulative += e.chance;
            if (roll < cumulative)
            {
                StartEvent(e);
                return true;
            }
        }

        return false;
    }

    private void StartEvent(GameEvent e)
    {
        ActiveEvent = e;
        activeTimer = e.duration;
        e.onStart?.Invoke();
    }

    public void EndEvent()
    {
        if (!IsEventActive) return;

        var e = ActiveEvent;
        ActiveEvent = null;
        e.lastEndTime = Time.time;
        e.onEnd?.Invoke();
    }

    private bool IsAvailable(GameEvent e)
    {
        return Time.time - e.lastEndTime >= e.cooldown;
    }
}
