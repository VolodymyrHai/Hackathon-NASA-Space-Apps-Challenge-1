using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.InputSystem;
public class AirdropSpawner : MonoBehaviour
{
    public int medicine;public int water;public int food;
    public void Init(Vector3 pos,int medicine, int water, int food)
    {
        transform.position = pos;
        this.medicine = medicine;
        this.water=water;
        this.food=food;
        Check();
    }
    private void Check()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, Vector3.down, out hit, 10f))
        {
            if(hit.collider.tag == "House")
            {
                transform.position = new Vector3(13+Random.Range(-100,100),20,13+Random.Range(-100,100));
                Check();
            }
        }
    }
}
