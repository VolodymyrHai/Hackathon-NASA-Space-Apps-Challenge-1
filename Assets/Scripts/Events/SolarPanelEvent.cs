using System;
using UnityEngine;

public class SolarPanelEvent : MonoBehaviour
{
    [SerializeField] private Messanger messanger;
    public void OnStart()
    {
        messanger.Show();
        EnergyCost.broken = true;
    }
}
