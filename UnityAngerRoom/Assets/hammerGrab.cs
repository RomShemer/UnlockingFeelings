//using UnityEngine;
//using UnityEngine.Networking;
//using System.Collections;
//using UnityEngine.XR.Interaction.Toolkit.Interactables;
//using UnityEngine.XR.Interaction.Toolkit;

//public class HammerSensorPoller : MonoBehaviour
//{
//    [SerializeField] private string serverUrl = "https://0499-62-90-179-254.ngrok-free.app/data";
//    public Transform hammer;
//    public float pollingInterval = 0.5f;
//    [SerializeField] private float rotationThreshold = 0.5f;
//    private XRGrabInteractable grab;
//    private Rigidbody rb;

//    private float lastPitch = 0f;
//    private float lastRoll = 0f;
//    private float lastYaw = 0f;

//    private void Start() 
//    {
//        StartCoroutine(PollSensorData());
//    }

//    private IEnumerator PollSensorData()
//    {
//        while (true)
//        {
//            UnityWebRequest request = UnityWebRequest.Get(serverUrl);
//            request.SetRequestHeader("ngrok-skip-browser-warning", "true");

//            yield return request.SendWebRequest();

//            if (request.result == UnityWebRequest.Result.Success)
//            {
//                string[] values = request.downloadHandler.text.Split(',');

//                if (values.Length == 3 &&
//                    float.TryParse(values[0], out float pitch) &&
//                    float.TryParse(values[1], out float roll) &&
//                    float.TryParse(values[2], out float yaw))
//                {
//                    if (Mathf.Abs(pitch - lastPitch) > rotationThreshold ||
//                        Mathf.Abs(roll - lastRoll) > rotationThreshold ||
//                        Mathf.Abs(yaw - lastYaw) > rotationThreshold)
//                    {
//                        hammer.rotation = Quaternion.Euler(pitch, yaw, roll);
//                        lastPitch = pitch;
//                        lastRoll = roll;
//                        lastYaw = yaw;
//                    }
//                }
//                else
//                {
//                    Debug.LogWarning($"❌ לא ניתן לפענח: {request.downloadHandler.text}");
//                }
//            }
//            else
//            {
//                Debug.LogWarning("⚠️ שגיאה בבקשה: " + request.error);
//            }

//            yield return new WaitForSeconds(pollingInterval);
//        }
//    }

//    private void Awake()
//    {
//        grab = GetComponent<XRGrabInteractable>();
//        rb = GetComponent<Rigidbody>();

//        if (grab == null || rb == null)
//        {
//            LogToConsole("❌ XRGrabInteractable או Rigidbody חסרים");
//            return;
//        }

//        grab.selectEntered.AddListener(OnGrab);
//        grab.selectExited.AddListener(OnRelease); // 🎯 הוספנו גם שחרור
//        LogToConsole("✅ Awake – מאזין נוסף");
//    }

//    public void OnGrab(SelectEnterEventArgs args)
//    {
//        LogToConsole("🤲 תפסנו את הקובייה!");
//        StartCoroutine(SendToServer());

//        // בזמן תפיסה – נעצור את הפיזיקה
//        rb.isKinematic = true;
//    }

//    public void OnRelease(SelectExitEventArgs args)
//    {
//        LogToConsole("👋 שחררנו את הקובייה, מפעילים פיזיקה");
//        rb.isKinematic = false;
//        rb.useGravity = true;
//        rb.WakeUp(); // להפעיל פיזיקה מיד
//    }

//    private IEnumerator SendToServer()
//    {
//        LogToConsole("📡 שולחת בקשה לשרת...");
//        UnityWebRequest request = UnityWebRequest.Get(serverUrl);
//        yield return request.SendWebRequest();

//        if (request.result != UnityWebRequest.Result.Success)
//            LogToConsole("❌ שגיאה: " + request.error);
//        else
//            LogToConsole("✅ הצלחה!");
//    }

//    private void LogToConsole(string message)
//    {
//        Debug.Log(message);
//        //if (consoleText != null)
//        //    consoleText.text = message;
//    }
//}
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

public class HammerSensorPoller : MonoBehaviour
{
    [SerializeField] private string serverUrl = "https://0499-62-90-179-254.ngrok-free.app/data";
    public Transform hammer;
    public float pollingInterval = 0.5f;
    [SerializeField] private float rotationThreshold = 1.5f;
    [SerializeField] private float smoothingFactor = 0.1f;

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private bool isGrabbed = false;

    private float lastPitch = 0f;
    private float lastRoll = 0f;
    private float lastYaw = 0f;

    private void Start()
    {
        Debug.Log("🚀 התחלה: PollSensorData");
        StartCoroutine(PollSensorData());
    }

    private bool IsValidSensorValue(float pitch, float roll, float yaw)
    {
        return Mathf.Abs(pitch) <= 180f && Mathf.Abs(roll) <= 180f && Mathf.Abs(yaw) <= 2000f;
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < -180f) angle += 360f;
        else if (angle > 180f) angle -= 360f;
        return angle;
    }

    private IEnumerator PollSensorData()
    {
        while (true)
        {
            // רק אם הפטיש לא מוחזק – נעדכן את הסיבוב
            if (!isGrabbed)
            {
                UnityWebRequest request = UnityWebRequest.Get(serverUrl);
                request.SetRequestHeader("ngrok-skip-browser-warning", "true");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    Debug.Log($"🌐 תשובת השרת: {response}");

                    string[] values = response.Split(',');

                    if (values.Length == 3 &&
                        float.TryParse(values[0], out float pitch) &&
                        float.TryParse(values[1], out float roll) &&
                        float.TryParse(values[2], out float yaw))
                    {
                        Debug.Log($"🔍 pitch={pitch}, roll={roll}, yaw={yaw}");

                        if (!IsValidSensorValue(pitch, roll, yaw))
                        {
                            Debug.LogWarning($"⚠️ ערכים לא תקפים – מדלגים");
                            yield return new WaitForSeconds(pollingInterval);
                            continue;
                        }

                        float deltaPitch = Mathf.Abs(pitch - lastPitch);
                        float deltaRoll = Mathf.Abs(roll - lastRoll);
                        float deltaYaw = Mathf.Abs(yaw - lastYaw);

                        if (deltaPitch > rotationThreshold || deltaRoll > rotationThreshold || deltaYaw > rotationThreshold)
                        {
                            lastPitch = Mathf.Lerp(lastPitch, pitch, smoothingFactor);
                            lastRoll = Mathf.Lerp(lastRoll, roll, smoothingFactor);
                            lastYaw = Mathf.Lerp(lastYaw, yaw, smoothingFactor);

                            float normalizedYaw = NormalizeAngle(lastYaw);
                            Quaternion targetRotation = Quaternion.Euler(lastPitch, normalizedYaw, lastRoll);

                            // ✅ השתמשי ב־MoveRotation בשביל יציבות
                            rb.MoveRotation(targetRotation);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"❌ לא ניתן לפענח את התשובה: {response}");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ שגיאה בבקשה: " + request.error);
                }
            }

            yield return new WaitForSeconds(pollingInterval);
        }
    }

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        if (grab == null || rb == null)
        {
            LogToConsole("❌ XRGrabInteractable או Rigidbody חסרים");
            return;
        }

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
        LogToConsole("✅ Awake – מאזין נוסף");
    }

    public void OnGrab(SelectEnterEventArgs args)
    {
        LogToConsole("🤲 תפסנו את הפטיש – עוצרים תנועת חיישן");
        isGrabbed = true;
        rb.isKinematic = false;
    }

    public void OnRelease(SelectExitEventArgs args)
    {
        LogToConsole("👋 שחררנו את הפטיש – מפעילים תנועת חיישן");
        isGrabbed = false;
        rb.useGravity = true;
        rb.WakeUp();
    }

    private IEnumerator SendToServer()
    {
        LogToConsole("📡 שולחת בקשה לשרת...");
        UnityWebRequest request = UnityWebRequest.Get(serverUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            LogToConsole("❌ שגיאה: " + request.error);
        else
            LogToConsole("✅ הצלחה!");
    }

    private void LogToConsole(string message)
    {
        Debug.Log(message);
    }
}
