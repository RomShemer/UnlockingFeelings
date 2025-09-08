using UnityEngine;

public enum BookType { Sadness, Anxiety, Fear, Love, Envy, Happiness, Anger }

[RequireComponent(typeof(Rigidbody))]
public class BookIdentity : MonoBehaviour
{
    public BookType type;

    Vector3 homePos;
    Quaternion homeRot;
    Rigidbody rb;

    void Awake()
    {
        homePos = transform.position;
        homeRot = transform.rotation;
        rb = GetComponent<Rigidbody>();
    }

    public void ResetToHome()
    {
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(homePos, homeRot);
        gameObject.SetActive(true);
    }
}