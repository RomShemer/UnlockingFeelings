using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class GroundStickAndProject : MonoBehaviour
{
    public LayerMask groundMask;          // ���� ��� �� ���� ���� (�� Default)
    public float snapDistance = 0.5f;     // �� ��� ���� ������ ����
    public float snapSpeed = 20f;         // ��� ��� ������ ����� �����
    public float probeRadius = 0.15f;     // ����� �-SphereCast
    public float maxSlopeAngle = 70f;     // ����� �����

    CharacterController cc;
    Vector3 lastGroundNormal = Vector3.up;
    bool grounded;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (groundMask.value == 0) groundMask = ~0; // �� �� �����, ���
    }

    void FixedUpdate()
    {
        // 1) ����� ����� ����� ��������
        Vector3 feet = transform.position + Vector3.up * (cc.radius + 0.02f);

        // 2) ����� ���� ���� (SphereCast ��� ���� ����� �� �������)
        if (Physics.SphereCast(feet, probeRadius, Vector3.down, out RaycastHit hit, snapDistance + 0.2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            lastGroundNormal = hit.normal;
            grounded = Vector3.Angle(hit.normal, Vector3.up) <= maxSlopeAngle;

            if (grounded)
            {
                // 3) ����� ����� �����: ����/����� �� �-Y �� ������� �� ������� ��� �� ������
                float targetFeetY = hit.point.y + cc.skinWidth;
                float currentFeetY = feet.y;
                float deltaY = targetFeetY - currentFeetY;
                if (Mathf.Abs(deltaY) > 0.001f)
                {
                    // ������ �� ����� ����� ����
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

    /// ����� ����� ����� ����� �� ������ �� ����� ��� ��� "����" ������
    public Vector3 ProjectOnGround(Vector3 desiredWorldMove)
    {
        if (!grounded) return desiredWorldMove;
        // ������ ���� ������ �������� ����� �� �����
        return Vector3.ProjectOnPlane(desiredWorldMove, lastGroundNormal);
    }
}
