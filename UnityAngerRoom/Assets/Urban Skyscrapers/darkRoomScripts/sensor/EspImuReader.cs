using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class EspImuReader : MonoBehaviour
{
    public string espUrl = "http://10.100.102.28/sensor";
    public Transform flashlight;   // האובייקט של הפנס בחדר
    public float updateInterval = 0.2f; // בקשה כל 50ms (20Hz)
    private Coroutine fetchCoroutine;
    public Transform handReference; // היד של השחקן (לדוגמה RightHand Anchor)
    public Transform flashlightGripPoint; // תגדירי את זה באינספקטור (נקודת האחיזה בתוך הפנס)

    private Quaternion initialRotationOffset;
    public float rotationSensitivity = 0.2f;

    void Start()
    {
        initialRotationOffset = Quaternion.Inverse(handReference.rotation) * flashlight.rotation;

        // מתחילים את הלולאה רק פעם אחת
        fetchCoroutine = StartCoroutine(FetchSensorLoop());
    }
    public void SetHandReference(Transform newHand)
    {
        handReference = newHand;
    }


    IEnumerator FetchSensorLoop()
    {
        while (true)
        {
            yield return StartCoroutine(GetSensorData());
            yield return new WaitForSeconds(updateInterval);
        }
    }

    //IEnumerator GetSensorData()
    //{
    //    using (UnityWebRequest   www = UnityWebRequest.Get(espUrl))
    //    {
    //        www.timeout = 2; // קיצור טיימאאוט למניעת תקיעות
    //        Debug.Log("Sending request to: " + espUrl);

    //        yield return www.SendWebRequest();

    //        if (www.result == UnityWebRequest.Result.Success)
    //        {
    //            Debug.Log("Response: " + www.downloadHandler.text);
    //            string json = www.downloadHandler.text;

    //            Debug.Log("Response: " + www.downloadHandler.text);

    //            SensorData data = JsonUtility.FromJson<SensorData>(json);

    //            float deltaTime = updateInterval;

    //            Quaternion targetRotation = Quaternion.Euler(
    //                               data.pitch,   // X-axis
    //                               data.yaw,     // Y-axis
    //                               data.roll     // Z-axis
    //                           );

    //             flashlight.localRotation = Quaternion.Slerp(
    //                flashlight.localRotation,
    //                targetRotation,
    //                rotationSensitivity
    //            );

    //            // הוספת הסיבוב היחסי לרוטציה הקיימת
    //            //flashlight.localRotation *= Quaternion.Euler(targetRotation);
    //            Debug.Log($"Δrot: {targetRotation} | New rot: {flashlight.localRotation.eulerAngles}");

    //            // שימוש בג'יירו להזזת הפנס
    //            //flashlight.localRotation = Quaternion.Euler(
    //            //    data.gx,   // Pitch
    //            //    data.gy,   // Yaw
    //            //    data.gz    // Roll
    //            //);
    //        }
    //        else
    //        {
    //            Debug.LogWarning("Failed to fetch sensor data: " + www.error);
    //        }
    //    }
    //}

    //IEnumerator GetSensorData()
    //{
    //    using (UnityWebRequest www = UnityWebRequest.Get(espUrl))
    //    {
    //        www.timeout = 2;
    //        yield return www.SendWebRequest();

    //        if (www.result == UnityWebRequest.Result.Success)
    //        {
    //            Debug.Log("Sending request to: " + espUrl);

    //            string json = www.downloadHandler.text;

    //            Debug.Log("Response: " + www.downloadHandler.text);

    //            SensorData data = JsonUtility.FromJson<SensorData>(json);

    //            // המרה של נתוני הסיבוב לזוויות יחסיות מנורמלות
    //            float deltaPitch = Mathf.Deg2Rad * data.pitch * rotationSensitivity;
    //            float deltaYaw = Mathf.Deg2Rad * data.yaw * rotationSensitivity;
    //            float deltaRoll = Mathf.Deg2Rad * data.roll * rotationSensitivity;

    //            // יוצרים שינוי סיבוב יחסי (delta rotation)
    //            Quaternion deltaRotation = Quaternion.Euler(deltaPitch, deltaYaw, deltaRoll);

    //            Debug.Log($"ΔRot: ({deltaPitch}, {deltaYaw}, {deltaRoll}) | New rot: {flashlight.localRotation.eulerAngles}");

    //            // מעדכנים את הרוטציה הנוכחית של הפנס ביחס לdelta
    //            flashlight.localRotation *= deltaRotation;

    //            Debug.Log($"ΔRot: ({deltaPitch}, {deltaYaw}, {deltaRoll}) | New rot: {flashlight.localRotation.eulerAngles}");
    //        }
    //        else
    //        {
    //            Debug.LogWarning("Failed to fetch sensor data: " + www.error);
    //        }
    //    }
    //}

    //IEnumerator GetSensorData()
    //{
    //    using (UnityWebRequest www = UnityWebRequest.Get(espUrl))
    //    {
    //        www.timeout = 5;
    //        yield return www.SendWebRequest();

    //        if (www.result == UnityWebRequest.Result.Success)
    //        {
    //            string json = www.downloadHandler.text;
    //            SensorData data = JsonUtility.FromJson<SensorData>(json);

    //            // נרמול זוויות Pitch, Roll, Yaw לטווח -180 עד 180
    //            float pitch = NormalizeAngle(data.pitch);
    //            float yaw = NormalizeAngle(data.yaw);
    //            float roll = NormalizeAngle(data.roll);

    //            // קובעים סיבוב מוחלט על פי הקריאות של החיישן
    //            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, roll);

    //            // מעבר חלק לסיבוב החדש (Slerp)
    //            flashlight.localRotation = Quaternion.Slerp(
    //                flashlight.localRotation,
    //                targetRotation,
    //                rotationSensitivity
    //            );

    //            Debug.Log($"Target Rot: ({pitch}, {yaw}, {roll}) | New rot: {flashlight.localRotation.eulerAngles}");
    //        }
    //        else
    //        {
    //            Debug.LogWarning("Failed to fetch sensor data: " + www.error);
    //        }
    //    }
    //}

    // פונקציה שמנרמלת כל זווית לטווח [-180, 180]

    //IEnumerator GetSensorData()
    //{
    //    using (UnityWebRequest www = UnityWebRequest.Get(espUrl))
    //    {
    //        www.timeout = 5;
    //        yield return www.SendWebRequest();

    //        if (www.result == UnityWebRequest.Result.Success)
    //        {
    //            string json = www.downloadHandler.text;
    //            SensorData data = JsonUtility.FromJson<SensorData>(json);

    //            float pitch = NormalizeAngle(data.pitch);
    //            float yaw = NormalizeAngle(data.yaw);
    //            float roll = NormalizeAngle(data.roll);

    //            Quaternion imuRotation = Quaternion.Euler(pitch, yaw, roll);

    //            // 1. הצמדת מיקום הפנס ליד
    //            flashlight.position = handReference.position;

    //            // 2. שילוב סיבוב היד עם סיבוב החיישן (סיבוב יחסי)
    //            flashlight.rotation = handReference.rotation * imuRotation;

    //            Debug.Log($"Target Rot: ({pitch}, {yaw}, {roll}) | New rot: {flashlight.rotation.eulerAngles}");
    //        }
    //        else
    //        {
    //            Debug.LogWarning("Failed to fetch sensor data: " + www.error);
    //        }
    //    }
    //}
    IEnumerator GetSensorData()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(espUrl))
        {
            www.timeout = 5;
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                SensorData data = JsonUtility.FromJson<SensorData>(json);

                float rawPitch = NormalizeAngle(data.pitch);
                float pitch = Mathf.Clamp(rawPitch, -60f, 60f);  // 🔥 לא יותר מדי למטה או למעלה
                //float pitch = NormalizeAngle(data.pitch);

                float yaw = NormalizeAngle(data.yaw);
                float roll = NormalizeAngle(data.roll);

                // הסיבוב של החיישן עצמו
                //Quaternion imuRotation = Quaternion.Euler(pitch, yaw, 0f);
                Quaternion imuRotation = Quaternion.Euler(0f, yaw, -pitch);

                Vector3 gripOffset = flashlight.position - flashlightGripPoint.position;
                flashlight.position = handReference.position + handReference.rotation * gripOffset;

                if (flashlight.position.y < 0.5f)
                {
                    flashlight.position = new Vector3(
                        flashlight.position.x,
                        0.5f,
                        flashlight.position.z
                    );
                }
                // מיקום: הצמדה ליד
                //flashlight.position = handReference.position;

                // סיבוב: יחסית ליד
                //flashlight.rotation = handReference.rotation * imuRotation;
                flashlight.rotation = handReference.rotation * imuRotation * initialRotationOffset;


                Debug.Log($"IMU: ({pitch}, {yaw}, {roll}) | Final Rotation: {flashlight.rotation.eulerAngles}");
            }
            else
            {
                Debug.LogWarning("Failed to fetch sensor data: " + www.error);
            }
        }
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        else if (angle < -180f)
            angle += 360f;
        return angle;
    }


    [System.Serializable]
    public class SensorData
    {
        public float pitch;
        public float roll;
        public float yaw;
    }
}

