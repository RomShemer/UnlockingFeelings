using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class FistPuncher : MonoBehaviour
{
    [Header("Force")]
    [Tooltip("כמה אימפולס (N·s) לכל 1 m/s של מהירות סגירה")]
    public float impulsePerMps = 3.0f;
    [Tooltip("מהירות סגירה מינימלית כדי להחשיב 'מכה'")]
    public float minClosingSpeed = 0.4f;
    public float cooldown = 0.05f;

    [Header("Controller (Meta/Oculus)")]
    public bool useOVRVel = true;
    public OVRInput.Controller which = OVRInput.Controller.RTouch;

    [Header("FX (optional)")]
    public AudioSource audioSource;
    public AudioClip[] hitClips;
    public float hapticAmp = 0.7f;
    public float hapticDur = 0.06f;

    Rigidbody rb;
    Vector3 lastPosWS;
    float lastHitTime = -999f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        lastPosWS = transform.position;
    }

    void FixedUpdate()
    {
        // שומר מיקום קודם למהירות ידנית במקרה שלא משתמשים ב-OVR
        lastPosWS = transform.position;
    }

    Vector3 GetWorldVelocity()
    {
        if (useOVRVel)
        {
            // מהירות לוקאלית של הקונטרולר → לעולם
            var vLocal = OVRInput.GetLocalControllerVelocity(which);
            var head = Camera.main ? Camera.main.transform : null;
            return head ? head.TransformVector(vLocal) : vLocal;
        }
        // חישוב מהירות ידנית
        return (transform.position - lastPosWS) / Mathf.Max(Time.fixedDeltaTime, 1e-4f);
    }

    void OnCollisionEnter(Collision c) => TryPunch(c);
    void OnCollisionStay(Collision c) => TryPunch(c);

    void TryPunch(Collision c)
    {
        if (Time.time - lastHitTime < cooldown) return;

        var otherRb = c.rigidbody;
        if (otherRb == null || otherRb.isKinematic) return; // צריך יעד דינמי (השק)

        Vector3 vSelf = GetWorldVelocity();
        float bestClosing = 0f;
        Vector3 bestPoint = c.GetContact(0).point;
        Vector3 bestNormal = c.GetContact(0).normal;

        foreach (var contact in c.contacts)
        {
            Vector3 vOther = otherRb.GetPointVelocity(contact.point);
            Vector3 rel = vSelf - vOther;
            // חיובי כשנכנסים פנימה לתוך האובייקט
            float closing = Vector3.Dot(-contact.normal, rel);
            if (closing > bestClosing)
            {
                bestClosing = closing;
                bestPoint = contact.point;
                bestNormal = contact.normal;
            }
        }

        if (bestClosing < minClosingSpeed) return;

        float J = bestClosing * impulsePerMps;
        otherRb.AddForceAtPosition(-bestNormal * J, bestPoint, ForceMode.Impulse);
        lastHitTime = Time.time;

        // FX
        if (audioSource && hitClips != null && hitClips.Length > 0)
            audioSource.PlayOneShot(hitClips[Random.Range(0, hitClips.Length)]);

        if (useOVRVel)
        {
            OVRInput.SetControllerVibration(1f, hapticAmp, which);
            StartCoroutine(StopHaptics());
        }
    }

    IEnumerator StopHaptics()
    {
        yield return new WaitForSeconds(hapticDur);
        OVRInput.SetControllerVibration(0f, 0f, which);
    }
}
