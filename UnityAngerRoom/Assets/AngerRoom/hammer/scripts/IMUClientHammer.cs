//using System.Collections;
//using UnityEngine;
//using UnityEngine.Networking;

//[DefaultExecutionOrder(-50)]
//public class IMUClientHammer : MonoBehaviour
//{
//    [Header("ESP Settings")]
//    [Tooltip("ה-IP של ה-ESP, ללא נתיב! למשל: http://172.20.10.2")]
//    public string baseUrl = "http://172.20.10.2";   // ← בלי /hammer
//    [Tooltip("הנתיב לפטיש ב-ESP")]
//    public string endpoint = "/hammer";
//    public float updateInterval = 0.05f;

//    [Header("Smoothing (EMA)")]
//    [Range(0f, 1f)] public float emaAlphaAccel = 0.2f;
//    [Range(0f, 1f)] public float emaAlphaGyro = 0.2f;

//    [HideInInspector] public float aMag_g;
//    [HideInInspector] public float wMag_dps;
//    [HideInInspector] public Vector3 accel_ms2;
//    [HideInInspector] public Vector3 gyro_dps;
//    [HideInInspector] public Vector3 euler;

//    float aMag_g_ema, wMag_dps_ema;

//    [System.Serializable]
//    public class HammerData
//    {
//        public float aMag_g, wMag_dps;
//        public float ax, ay, az;
//        public float gx, gy, gz;
//        public float pitch, roll, yaw;
//    }

//    void OnEnable() => StartCoroutine(PollLoop());
//    void OnDisable() => StopAllCoroutines();

//    IEnumerator PollLoop()
//    {
//        while (true)
//        {
//            var url = baseUrl.TrimEnd('/') + endpoint; // דוגמה: http://172.20.10.2/hammer
//            using (var www = UnityWebRequest.Get(url))
//            {
//                www.timeout = 5;
//                yield return www.SendWebRequest();

//                if (www.result == UnityWebRequest.Result.Success)
//                {
//                    try
//                    {
//                        var d = JsonUtility.FromJson<HammerData>(www.downloadHandler.text);
//                        accel_ms2 = new Vector3(d.ax, d.ay, d.az);
//                        gyro_dps = new Vector3(d.gx, d.gy, d.gz);
//                        euler = new Vector3(d.pitch, d.roll, d.yaw);
//                        aMag_g_ema = Mathf.Lerp(aMag_g_ema, d.aMag_g, emaAlphaAccel);
//                        wMag_dps_ema = Mathf.Lerp(wMag_dps_ema, d.wMag_dps, emaAlphaGyro);
//                        aMag_g = aMag_g_ema;
//                        wMag_dps = wMag_dps_ema;
//                    }
//                    catch (System.Exception e)
//                    {
//                        Debug.LogWarning("IMUClientHammer parse error: " + e.Message);
//                    }
//                }
//                else
//                {
//                    Debug.LogWarning($"IMUClientHammer HTTP error ({www.responseCode}) on {url}: {www.error}");
//                }
//            }
//            yield return new WaitForSeconds(updateInterval);
//        }
//    }
//}

using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[DefaultExecutionOrder(-50)]
public class IMUClientHammer : MonoBehaviour
{
    [Header("ESP Settings")]
    [Tooltip("ה-IP של ה-ESP, ללא נתיב! למשל: http://172.20.10.2")]
    public string baseUrl = "http://192.168.4.1";
    [Tooltip("הנתיב לפטיש ב-ESP")]
    public string endpoint = "/hammer";
    [Tooltip("פולינג הרץ (שניות). 0.05 = ~20Hz")]
    public float updateInterval = 0.05f;
    [Tooltip("Timeout לבקשה ב-שניות")]
    public int timeoutSeconds = 5;

    [Header("Smoothing (EMA)")]
    [Range(0f, 1f)] public float emaAlphaAccel = 0.2f;
    [Range(0f, 1f)] public float emaAlphaGyro = 0.2f;

    [Header("Fallback (optional)")]
    public bool enableFallbackEndpoint = true;
    public string fallbackEndpoint = "/sensor";
    [Tooltip("כמה כשלונות רצופים לפני מעבר לפולבאק")]
    public int switchAfterFailures = 5;

    // נתונים לצריכה חיצונית
    [HideInInspector] public float aMag_g;      // גודל תאוצה ב-g
    [HideInInspector] public float wMag_dps;    // גודל מהירות זוויתית ב-deg/s
    [HideInInspector] public Vector3 accel_ms2; // m/s^2
    [HideInInspector] public Vector3 gyro_dps;  // deg/s
    [HideInInspector] public Vector3 euler;     // pitch/roll/yaw (deg)

    // פנימי
    float aMag_g_ema, wMag_dps_ema;
    bool _emaInitialized;
    int _failCount;
    string _currentEndpoint;
    bool _loggedUrl;

    [System.Serializable]
    public class HammerData
    {
        public float aMag_g, wMag_dps;
        public float ax, ay, az;
        public float gx, gy, gz;
        public float pitch, roll, yaw;
    }

    Coroutine _loopCo;

    void OnEnable()
    {
        _currentEndpoint = endpoint;
        _failCount = 0;
        _emaInitialized = false;
        _loopCo = StartCoroutine(PollLoop());
    }

    void OnDisable()
    {
        if (_loopCo != null) StopCoroutine(_loopCo);
        _loopCo = null;
    }

    IEnumerator PollLoop()
    {
        var wait = new WaitForSecondsRealtime(updateInterval);

        while (true)
        {
            string url = baseUrl.TrimEnd('/') + _currentEndpoint;

            if (!_loggedUrl)
            {
                Debug.Log("[IMU] polling " + url);
                _loggedUrl = true;
            }

            using (var www = UnityWebRequest.Get(url))
            {
                www.timeout = timeoutSeconds;
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    _failCount = 0;

                    try
                    {
                        var txt = www.downloadHandler.text;
                        var d = JsonUtility.FromJson<HammerData>(txt);

                        // נתוני בסיס
                        accel_ms2 = new Vector3(d.ax, d.ay, d.az);
                        gyro_dps = new Vector3(d.gx, d.gy, d.gz);
                        euler = new Vector3(d.pitch, d.roll, d.yaw);

                        // חישובי גיבוי אם חסר ב-JSON
                        float aMag = d.aMag_g;
                        if (aMag <= 0f && (accel_ms2 != Vector3.zero))
                            aMag = accel_ms2.magnitude / 9.80665f;

                        float wMag = d.wMag_dps;
                        if (wMag <= 0f && (gyro_dps != Vector3.zero))
                            wMag = gyro_dps.magnitude;

                        // אתחול EMA על המדידה הראשונה כדי להימנע מדיליי
                        if (!_emaInitialized)
                        {
                            aMag_g_ema = aMag;
                            wMag_dps_ema = wMag;
                            _emaInitialized = true;
                        }
                        else
                        {
                            aMag_g_ema = Mathf.Lerp(aMag_g_ema, aMag, emaAlphaAccel);
                            wMag_dps_ema = Mathf.Lerp(wMag_dps_ema, wMag, emaAlphaGyro);
                        }

                        // סניטיזציה
                        if (float.IsNaN(aMag_g_ema) || float.IsInfinity(aMag_g_ema)) aMag_g_ema = aMag;
                        if (float.IsNaN(wMag_dps_ema) || float.IsInfinity(wMag_dps_ema)) wMag_dps_ema = wMag;

                        aMag_g = aMag_g_ema;
                        wMag_dps = wMag_dps_ema;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("[IMU] Parse error: " + e.Message);
                    }
                }
                else
                {
                    _failCount++;
                    Debug.LogWarning($"[IMU] HTTP err ({www.responseCode}) on {url}: {www.error} (fail={_failCount})");

                    if (enableFallbackEndpoint &&
                        _failCount >= switchAfterFailures &&
                        _currentEndpoint != fallbackEndpoint)
                    {
                        _currentEndpoint = fallbackEndpoint;
                        _failCount = 0;
                        _loggedUrl = false; // נדפיס את ה-URL החדש
                        Debug.Log("[IMU] switching endpoint to fallback: " + _currentEndpoint);
                    }
                }
            }

            yield return wait;
        }
    }
}

