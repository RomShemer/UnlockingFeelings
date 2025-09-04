using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DisappearOnGrab : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    public ParticleSystem disappearEffect; // אפקט של התפוגגות (גררי את זה באינספקטור)

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        // ניצור אפקט של התפוגגות
        if (disappearEffect != null)
        {
            Instantiate(disappearEffect, transform.position, Quaternion.identity);
        }

        // נעלים את האובייקט (אפשר גם לעשות Destroy(gameObject);)
        gameObject.SetActive(false);
    }
}
