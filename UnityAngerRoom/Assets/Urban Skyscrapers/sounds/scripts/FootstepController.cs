using UnityEngine;
using UnityEngine.Audio;

public class FootstepController : MonoBehaviour
{
    [Header("Refs")]
    public Transform rigRoot;                 // XR Origin / Camera Rig (אם ריק, נשתמש ב-transform שלנו)
    public AudioSource audioSource;           // AudioSource שיושב על ה-Rig
    public AudioMixerGroup outputGroup;       // אופציונלי: לחבר ל-SFX במיקסר

    [Header("Clips")]
    public AudioClip[] bridgeSteps;           // קליפים של צעדים על גשר/עץ

    [Header("Step Logic")]
    public float baseStepInterval = 0.6f;     // מרווח צעדים בהליכה איטית (שניות)
    public float minSpeedToStep = 0.1f;       // מהירות מינ' להשמעת צעדים (m/s)
    public float maxSpeedForScale = 3f;       // מהירות שמעליה לא מקצרים עוד את המרווח

    [Header("Ground Check")]
    public string bridgeTag = "bridge";       // ← התגית שצריך לפגוע בה
    public LayerMask groundMask = ~0;         // שכבות קרקע (ברירת מחדל: הכול)
    public float groundCheckRadius = 0.2f;    // רדיוס בדיקת קרקע
    public float groundCheckOffset = 0.1f;    // כמה מתחת ל-rig לבדוק

    [Header("Randomization")]
    public Vector2 volumeJitter = new Vector2(0.18f, 0.28f);
    public Vector2 pitchJitter = new Vector2(0.95f, 1.05f);

    private Vector3 _prevPos;
    private float _timer;
    private CharacterController _cc;

    void Awake()
    {
        if (!rigRoot) rigRoot = transform;
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        if (audioSource && outputGroup) audioSource.outputAudioMixerGroup = outputGroup;

        _prevPos = rigRoot.position;
        _timer = baseStepInterval;

        // נאתר CharacterController אם קיים (XR Origin/Player)
        _cc = rigRoot.GetComponent<CharacterController>();
        if (!_cc) _cc = rigRoot.GetComponentInParent<CharacterController>();
    }

    void Update()
    {
        // מהירות אופקית (נעזר ב-CharacterController אם יש, אחרת לפי תזוזת טרנספורם)
        float speed;
        if (_cc != null)
        {
            Vector3 v = _cc.velocity;
            speed = new Vector3(v.x, 0f, v.z).magnitude;
        }
        else
        {
            Vector3 delta = rigRoot.position - _prevPos;
            speed = new Vector3(delta.x, 0f, delta.z).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            _prevPos = rigRoot.position;
        }

        if (IsOnBridge() && speed > minSpeedToStep)
        {
            // קצב הצעדים מתקצר כשזזים מהר
            float speedScale = Mathf.Clamp(speed / maxSpeedForScale, 0.3f, 2.5f);
            _timer -= Time.deltaTime * speedScale;

            if (_timer <= 0f)
            {
                PlayFootstep();
                _timer = baseStepInterval;
            }
        }
        else
        {
            // לא זזים/לא על הגשר – מחכים
            _timer = Mathf.Min(_timer, baseStepInterval);
        }
    }

    bool IsOnBridge()
    {
        // נבדוק מגע בקרקע + שהמשטח מתויג "bridge"
        Vector3 origin = rigRoot.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider != null && hit.collider.CompareTag(bridgeTag);
        }
        // גיבוי: בדיקת נפח קטנה מתחת לריג
        return Physics.CheckSphere(rigRoot.position + Vector3.down * groundCheckOffset, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
    }

    void PlayFootstep()
    {
        if (audioSource == null || bridgeSteps == null || bridgeSteps.Length == 0) return;

        audioSource.pitch = Random.Range(pitchJitter.x, pitchJitter.y);
        float vol = Random.Range(volumeJitter.x, volumeJitter.y);
        audioSource.PlayOneShot(bridgeSteps[Random.Range(0, bridgeSteps.Length)], vol);
    }

    void OnDrawGizmosSelected()
    {
        if (!rigRoot) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(rigRoot.position + Vector3.down * groundCheckOffset, groundCheckRadius);
    }
}
