using UnityEngine;

public static class StaticBarsLogic
{
    public static BarsHpLogic model;
    public static void Damage(BodyPartType type,int damage) { if(model!=null) model.ChangeDamage(type,damage); }
    public static void Heal(BodyPartType type,int heal) { if(model!=null) model.ChangeHeal(type,heal); }
    public static void Break(BodyPartType type,bool broken) { if(model!=null) model.ChangeBroken(type,broken); }

    static public RadBarsLogic rad;
    static public void Oxygen( int change)
    {
        // 0 або 1 для зміни
        rad.oxygenChange = change;
    }
    static public void Radiation(int change)
    {
        // -1 або 0 або 1 для зміни
        rad.radiationChange = change;
    }
}
