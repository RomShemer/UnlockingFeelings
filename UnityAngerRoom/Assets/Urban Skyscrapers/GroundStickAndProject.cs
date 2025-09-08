using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class GroundStickAndProject : MonoBehaviour
{
    public LayerMask groundMask;          // שימי כאן את שכבת הגשר (או Default)
    public float snapDistance = 0.5f;     // עד כמה מותר “לשאוב” למטה
    public float snapSpeed = 20f;         // כמה מהר להצמיד לגובה הקרקע
    public float probeRadius = 0.15f;     // רדיוס ל-SphereCast
    public float maxSlopeAngle = 70f;     // מגבלת שיפוע

    CharacterController cc;
    Vector3 lastGroundNormal = Vector3.up;
    bool grounded;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (groundMask.value == 0) groundMask = ~0; // אם לא הוגדר, הכל
    }

    void FixedUpdate()
    {
        // 1) נגדיר נקודת בדיקה מהרגליים
        Vector3 feet = transform.position + Vector3.up * (cc.radius + 0.02f);

        // 2) נבדוק קרקע מתחת (SphereCast קצת יותר סלחני על קימורים)
        if (Physics.SphereCast(feet, probeRadius, Vector3.down, out RaycastHit hit, snapDistance + 0.2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            lastGroundNormal = hit.normal;
            grounded = Vector3.Angle(hit.normal, Vector3.up) <= maxSlopeAngle;

            if (grounded)
            {
                // 3) הצמדה לגובה הקרקע: נעלה/נוריד את ה-Y של הקפסולה כך שהתחתית תשב על הפוינט
                float targetFeetY = hit.point.y + cc.skinWidth;
                float currentFeetY = feet.y;
                float deltaY = targetFeetY - currentFeetY;
                if (Mathf.Abs(deltaY) > 0.001f)
                {
                    // מזיזים רק אנכית בצורה חלקה
                    Vector3 move = new Vector3(0f, deltaY, 0f);
                    cc.Move(move * Mathf.Clamp01(Time.fixedDeltaTime * snapSpeed));
                }
            }
        }
        else
        {
            grounded = false;
        }
    }

    /// מקרין וקטור תנועה אופקי על הטנגנט של הקרקע כדי שלא "נרחף" בשיפוע
    public Vector3 ProjectOnGround(Vector3 desiredWorldMove)
    {
        if (!grounded) return desiredWorldMove;
        // מסירים רכיב בנורמל ומשאירים תנועה על המשטח
        return Vector3.ProjectOnPlane(desiredWorldMove, lastGroundNormal);
    }
}
