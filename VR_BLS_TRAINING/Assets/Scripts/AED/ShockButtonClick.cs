using UnityEngine;

public class ShockButtonClick : MonoBehaviour
{
    public AEDController controller;

    void OnMouseDown()
    {
        controller.DeliverShock();
    }
}
