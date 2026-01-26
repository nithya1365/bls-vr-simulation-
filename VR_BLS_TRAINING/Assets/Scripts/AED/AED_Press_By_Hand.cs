using UnityEngine;

public class AED_Press_By_Hand : MonoBehaviour
{
    [Header("Assign These")]
    public Transform rightHand;           // RightHandAnchor
    public Transform buttonReference;     // AED_Button_Reference
    public AEDController controller;

    bool pressing = false;

    void Update()
    {
        if (rightHand == null || buttonReference == null || controller == null)
            return;

        float handY = rightHand.position.y;
        float buttonY = buttonReference.position.y;

        // START PRESS (like CPR start)
        if (!pressing && handY < buttonY)
        {
            pressing = true;
            controller.OnAEDPowerOn();
            Debug.Log("🔘 AED BUTTON PRESSED");
        }

        // RELEASE (like CPR release)
        if (pressing && handY > buttonY)
        {
            pressing = false;
            Debug.Log("🔓 AED BUTTON RELEASED");
        }
    }
}
