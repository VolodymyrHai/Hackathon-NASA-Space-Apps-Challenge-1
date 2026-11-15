using System;
using UnityEngine;

public class EnergyCost : MonoBehaviour
{
    public static bool broken;

    [Header("ZoneToBreath")]
    [SerializeField] private GameObject[] zone;
    [SerializeField] private CommandCentreInteraction energy;

    [SerializeField] private float timer;
    [SerializeField] private float hm;
    private bool ok = true;

    private float time;
    void Update()
    {
        if(broken)
        {
            ok=true;
            energy.isSolarPanelActive = false;
            time += Time.deltaTime;
            if (time >= timer)
            {
                time-=timer;
                energy.energy=Math.Max(energy.energy-hm,0);
                if (energy.energy == 0)
                {
                    OffZone();
                }
            }
        }
        else 
        {
            if (ok){
                ok=false;
                OnZone();
            }
            energy.isSolarPanelActive = true;
            time += Time.deltaTime;
            if (time >= timer)
            {
                time-=timer;
                energy.energy=Math.Min(energy.energy+hm,100);
            }
        }
    }
    private void OffZone()
    {
        StaticBarsLogic.Oxygen(0);
        for(int i =0; i < zone.Length; i++)
        {
            zone[i].SetActive(false);
        }
    }
    private void OnZone()
    {
        for(int i =0; i < zone.Length; i++)
        {
            zone[i].SetActive(true);
        }
    }

}
