using UnityEngine;

public class OnButtonClick : MonoBehaviour
{
    public AEDController controller;

    void OnMouseDown()
    {
        controller.OnAEDPowerOn();
    }
}
