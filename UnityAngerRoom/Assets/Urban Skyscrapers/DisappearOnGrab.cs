using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DisappearOnGrab : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    public ParticleSystem disappearEffect; // ���� �� �������� (���� �� �� ����������)

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        // ����� ���� �� ��������
        if (disappearEffect != null)
        {
            Instantiate(disappearEffect, transform.position, Quaternion.identity);
        }

        // ����� �� �������� (���� �� ����� Destroy(gameObject);)
        gameObject.SetActive(false);
    }
}
