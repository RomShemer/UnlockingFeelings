using UnityEngine;

public class SnapToCurvedBridge : MonoBehaviour
{
    public Transform playerBody;           // ���� ����� ������ ����
    public float rayHeightOffset = 0.5f;   // ����� ���� ����� �� ���� ����
    public float distanceFromGround = 0.0f; // ���� ������ (���� �� ����� ���� ����)
    public LayerMask bridgeLayer;          // ���� �� ����
    public float playerHeight = 1.6f;  // ���� �������


    void Update()
    {
        Ray ray = new Ray(playerBody.position + Vector3.up * 2f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, rayHeightOffset, bridgeLayer))
        {
            Vector3 targetPosition = playerBody.position;
            targetPosition.y = hit.point.y + playerHeight;  // ������� ����
            playerBody.position = targetPosition;
        }
    }
}
