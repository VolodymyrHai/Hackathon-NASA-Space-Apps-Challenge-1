using System;
using UnityEngine;

public static class StaticBarsLogic
{
    public static Death d;
    public static bool entry =false;
    public static void Damage(BodyPartType type,int damage) { if(model!=null) model.ChangeDamage(type,damage); }
    public static void Heal(BodyPartType type,int heal) { if(model!=null) model.ChangeHeal(type,heal); }
    public static void Break(BodyPartType type,bool broken) { if(model!=null) model.ChangeBroken(type,broken); }

    static public BarsHpLogic model;
    static public RadBarsLogic rad;
    static public BarsLogic bars;
    static public int rada;
    static public void Oxygen( int change)
    {
        // 0 або 1 для зміни
        rad.oxygenChange = change;
    }
    static public void Radiation(int change)
    {
        Debug.Log(change);
        if (change > 0)
        {
            rada = change;
        }
        Debug.Log(change);
        rad.radiationChange = change;
    }
    static public void ChangeThirst(int change)
    {
        bars.ChangeThirst(change);
    }
    static public void ChangeHunger(int change)
    {
        bars.ChangeHunger(change);
    }
    static public void ChangeMind(int change)
    {
        bars.ChangeMind(change);
    }
}
