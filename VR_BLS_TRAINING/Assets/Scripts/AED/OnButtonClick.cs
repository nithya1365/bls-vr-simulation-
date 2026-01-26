using UnityEngine;

public class OnButtonClick : MonoBehaviour
{
    public AEDController controller;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform == transform)
                {
                    controller.OnAEDPowerOn();
                }
            }
        }
    }
}
