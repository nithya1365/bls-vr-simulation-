using UnityEngine;

public class AEDController : MonoBehaviour
{
    public AEDPad pad1;
    public AEDPad pad2;
    public GameObject shockButton;

    void Start()
    {
        // Shock button should be hidden at start
        shockButton.SetActive(false);
    }

    void Update()
    {
        // If both pads are placed correctly, enable shock
        if (pad1.placedCorrectly && pad2.placedCorrectly)
        {
            shockButton.SetActive(true);
        }
    }

    public void DeliverShock()
    {
        Debug.Log("Shock Delivered");
    }
}
