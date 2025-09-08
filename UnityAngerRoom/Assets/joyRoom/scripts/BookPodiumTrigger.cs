// Assets/Scripts/BookPodiumTrigger.cs
using UnityEngine;

public class BookPodiumTrigger : MonoBehaviour
{
    [Tooltip("אם ריק – יחפש BookVariantSwitcher על האובייקט שנכנס")]
    public BookVariantSwitcher book;

    private void OnTriggerEnter(Collider other)
    {
        var b = book ? book : other.GetComponentInParent<BookVariantSwitcher>();
        if (b) b.Open();
    }

    private void OnTriggerExit(Collider other)
    {
        var b = book ? book : other.GetComponentInParent<BookVariantSwitcher>();
        // אם רוצים שייסגר כשהספר יורד:
        // if (b) b.Close();
    }
}