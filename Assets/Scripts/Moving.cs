using UnityEngine;
using UnityEngine.InputSystem;

public class Moving : MonoBehaviour
{
    Rigidbody rg;
    float rotate;
    public int speed = 5;
    void Start()
    {
        rg = GetComponent<Rigidbody>();
        Cursor.lockState=CursorLockMode.Locked;
    }
    void Update()
    {
        rotate += Mouse.current.delta.x.ReadValue();
        rg.MoveRotation(Quaternion.Euler(0,rotate,0));

        float xMove = 0f;
        float zMove = 0f;

        if(Keyboard.current.dKey.isPressed) xMove =1f;
        if(Keyboard.current.aKey.isPressed) xMove =-1f;
        if(Keyboard.current.wKey.isPressed) zMove=1f;
        if(Keyboard.current.sKey.isPressed) zMove=-1f;

        rg.linearVelocity=transform.TransformDirection(new Vector3(xMove*speed,rg.linearVelocity.y,zMove*speed));
    }
}
