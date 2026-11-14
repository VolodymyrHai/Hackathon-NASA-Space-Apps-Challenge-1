using UnityEngine;

public class SunFollow : MonoBehaviour
{
    public Light bodyLight;             
    [Range(-180, 180)] public float azimuth = 30f;  
    [Range(-90, 90)]   public float elevation = 13f; 
    public float distance = 1000f;          
    public float size = 20f;               

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 dir = Quaternion.Euler(-elevation, azimuth, 0) * Vector3.forward;

        transform.position = cam.transform.position + dir * distance;
        transform.localScale = Vector3.one * size;

        if (bodyLight != null)
            bodyLight.transform.rotation = Quaternion.LookRotation(-dir);
    }
}
