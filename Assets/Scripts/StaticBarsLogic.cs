using UnityEngine;

static public class StaticBarsLogic
{
    static public BarsHpLogic model;
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
