using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Existing enum values and names remain compatible with scene events.
public enum BodyPartType {leftLeg,rigthLeg,leftArm,rigthArm,head,body}

public class BarsHpLogic : MonoBehaviour
{
    readonly Dictionary<BodyPartType,BodyPart> body = new Dictionary<BodyPartType,BodyPart>();
    public IReadOnlyDictionary<BodyPartType, BodyPart> Body => body;
    [SerializeField] private Image[] images;
    [Tooltip("Optional separate fracture overlays; never use the health image itself.")]
    [SerializeField] private Image[] imagesBroken;

    public void Awake()
    {
        StaticBarsLogic.model = this;
        body.Clear();
        int[] health = {50,50,50,50,30,70};
        for(int i=0;i<health.Length;i++)
        {
            Image image = images != null && i < images.Length ? images[i] : null;
            Image overlay = imagesBroken != null && i < imagesBroken.Length ? imagesBroken[i] : null;
            body[(BodyPartType)i] = new BodyPart(health[i],health[i],false,image,overlay);
        }
    }
    public bool HasInjuries
    {
        get { foreach (var part in body.Values) if (part.HP < part.maxHP || part.broken) return true; return false; }
    }
    public void RestoreBody()
    {
        foreach (var part in body.Values) { part.Heal(Mathf.CeilToInt(part.maxHP)); part.SetBroken(false); }
    }
    void OnDestroy() { if (StaticBarsLogic.model == this) StaticBarsLogic.model = null; }
    public void ChangeDamage(BodyPartType type,int damage)
    { if(body.TryGetValue(type,out BodyPart part)) part.Damage(Mathf.Max(0,damage)); }
    public void ChangeHeal(BodyPartType type,int heal)
    { if(body.TryGetValue(type,out BodyPart part)) part.Heal(Mathf.Max(0,heal)); }
    public void ChangeBroken(BodyPartType type,bool broken)
    { if(body.TryGetValue(type,out BodyPart part)) part.SetBroken(broken); }
    void Update()
    {
        if(body[BodyPartType.head].HP==0||body[BodyPartType.body].HP==0||(body[BodyPartType.rigthArm].HP==0 && body[BodyPartType.leftArm].HP==0 && body[BodyPartType.rigthLeg].HP==0 && body[BodyPartType.leftLeg].HP==0))
            StaticBarsLogic.d.SmertjVNishchitje();
    }
}

public class BodyPart
{
    public float HP {get;private set;}
    public float maxHP {get;}
    public bool broken {get;private set;}
    public Image image {get;}
    public Image imagesBroken {get;}
    public BodyPart(int HP,int maxHP,bool broken,Image image,Image imagesBroken)
    {
        this.maxHP = Mathf.Max(1,maxHP);
        this.HP = Mathf.Clamp(HP,0,this.maxHP);
        this.broken = broken; this.image = image;
        this.imagesBroken = imagesBroken != image ? imagesBroken : null;
        Refresh();
    }
    public void Damage(int damage) {         HP = Mathf.Max(0,HP-Mathf.Max(0,damage));Refresh(); Debug.Log(HP);}
    public void Heal(int heal) { HP = Mathf.Min(maxHP,HP+Mathf.Max(0,heal));Refresh(); }
    public void SetBroken(bool value) { broken=value;Refresh(); }
    void Refresh()
    {
        if(image != null)
        {
            Color color = Color.Lerp(Color.red,Color.green,HP/maxHP);
            if(broken) color=Color.Lerp(color,new Color(1,.5f,0,1),.55f);
            color.a=1; image.color=color;
        }
        if(imagesBroken != null) imagesBroken.gameObject.SetActive(broken);
    }
}
