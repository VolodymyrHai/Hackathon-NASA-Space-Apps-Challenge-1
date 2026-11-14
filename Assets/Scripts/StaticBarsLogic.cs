using UnityEngine;

static public class StaticBarsLogic
{
    static public BarsHpLogic model;
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
    static public void Damage(BodyPartType type,int damage)
    {
        model.ChangeDamage(type,damage);
    }
    static public void Heal(BodyPartType type,int heal)
    {
        model.ChangeHeal(type,heal);
    }
    static public void Break(BodyPartType type,bool broken)
    {
        model.ChangeBroken(type,broken);
    }
}
