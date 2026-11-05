using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BodyPartType {leftLeg,rigthLeg,leftArm,rigthArm,head,body}

public class BarsHpLogic : MonoBehaviour
{
    float thirstTimer = 0f;
    private Dictionary<BodyPartType,BodyPart> body = new Dictionary<BodyPartType, BodyPart>();
    [SerializeField] private Image[] images;
    private Image[] imagesBroken;
    
    public void Awake()
    {
        StaticBarsLogic.model = this;
        imagesBroken = new Image[images.Length];
        for(int i = 0; i < images.Length; i++)
        {
            imagesBroken[i] = images[i].GetComponentInChildren<Image>();
        }
        body.Add(BodyPartType.leftLeg,new(50,50,false,images[0],imagesBroken[0]));
        body.Add(BodyPartType.rigthLeg,new(50,50,false,images[1],imagesBroken[1]));
        body.Add(BodyPartType.leftArm,new(50,50,false,images[2],imagesBroken[2]));
        body.Add(BodyPartType.rigthArm,new(50,50,false,images[3],imagesBroken[3]));
        body.Add(BodyPartType.head,new(30,30,false,images[4],imagesBroken[4]));
        body.Add(BodyPartType.body,new(70,70,false,images[5],imagesBroken[5]));


    }
    public void ChangeDamage(BodyPartType type, int damage)
    {
        body[type].Damage(damage);
        body[type].image.color = new Color();
    }
    public void ChangeHeal(BodyPartType type, int heal)
    {
        body[type].Heal(heal);        
    }
    public void ChangeBroken(BodyPartType type, bool broken)
    {
        body[type].SetBroken(broken);        
    }
    

}

public class BodyPart
{
    public float HP {get;private set;}
    public float maxHP {get;}
    public bool broken {get;private set;}
    public Image image {get;}
    public Image imagesBroken {get;}

    public BodyPart(int HP, int maxHP, bool broken,Image image,Image imagesBroken)
    {
        this.HP = HP;
        this.broken = broken;
        this.maxHP = maxHP;
        this.image = image;
        this.imagesBroken = imagesBroken;
    }

    public void Damage(int damage)
    {
        HP = Math.Max(0,HP-damage);
        image.color = new Color(1-HP/maxHP,HP/maxHP,0);
    }
    public void Heal(int heal)
    {
        HP = Math.Min(maxHP,HP+heal);
        image.color = new Color(1-HP/maxHP,HP/maxHP,0);
    }
    public void SetBroken(bool broken)
    {
        this.broken = broken;
        imagesBroken.gameObject.SetActive(broken);
    }
}
