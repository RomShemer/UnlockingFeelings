using UnityEngine;

public class ReleaseButtonHandler : MonoBehaviour
{
    public float delay = 0f; // אם תרצה עיכוב קטן לפני ההיעלמות

    public void Hide()
    {
        if (delay <= 0f) gameObject.SetActive(false);
        else StartCoroutine(HideCo());
    }

    System.Collections.IEnumerator HideCo()
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}