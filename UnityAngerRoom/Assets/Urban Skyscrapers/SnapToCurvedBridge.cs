using UnityEngine;

public class SnapToCurvedBridge : MonoBehaviour
{
    public Transform playerBody;           // הגוף שצריך להתאים לגשר
    public float rayHeightOffset = 0.5f;   // מאיזה גובה לזרוק את הקרן למטה
    public float distanceFromGround = 0.0f; // מרחק מהרצפה (למשל אם רוצים לרחף טיפה)
    public LayerMask bridgeLayer;          // לייר של הגשר
    public float playerHeight = 1.6f;  // גובה דיפולטי


    void Update()
    {
        Ray ray = new Ray(playerBody.position + Vector3.up * 2f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, rayHeightOffset, bridgeLayer))
        {
            Vector3 targetPosition = playerBody.position;
            targetPosition.y = hit.point.y + playerHeight;  // מוסיפים גובה
            playerBody.position = targetPosition;
        }
    }
}
