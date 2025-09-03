using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

[Serializable]
public class ImuSample {
    public float pitch, roll, yaw;
    public float ax, ay, az;
}

public class EspImuClient : MonoBehaviour {
    [Header("ESP Endpoint")]
    public string sensorUrl = "http://esp-imu.local/sensor";
    [Range(0.02f, 0.2f)] public float interval = 0.05f;
    public bool logErrors = true;

    [Header("Debug HUD")]
    public bool showHud = true;

    public ImuSample Latest { get; private set; }
    public bool HasSample { get; private set; }

    public event Action<ImuSample> OnSample;

    Coroutine loop;
    float _lastPrint;

    void OnEnable() { loop = StartCoroutine(PollLoop()); }
    void OnDisable() { if (loop != null) StopCoroutine(loop); loop = null; }

    IEnumerator PollLoop() {
        var wait = new WaitForSeconds(interval);
        while (true) {
            using (var req = UnityWebRequest.Get(sensorUrl)) {
                req.downloadHandler = new DownloadHandlerBuffer();
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success) {
                    try {
                        var sample = JsonUtility.FromJson<ImuSample>(req.downloadHandler.text);
                        if (sample != null) {
                            Latest = sample;
                            HasSample = true;
                            OnSample?.Invoke(sample);
                            if (Time.time - _lastPrint > 1f) {
                                Debug.Log($"[EspImuClient] pitch={sample.pitch:F1} roll={sample.roll:F1} | ax={sample.ax:F2} ay={sample.ay:F2} az={sample.az:F2}");
                                _lastPrint = Time.time;
                            }
                        }
                    } catch (Exception e) {
                        if (logErrors) Debug.LogWarning($"[EspImuClient] JSON parse error: {e.Message}");
                    }
                } else {
                    if (logErrors) Debug.LogWarning($"[EspImuClient] HTTP error: {req.error}");
                    HasSample = false;
                }
            }
            yield return wait;
        }
    }

    void OnGUI() {
        if (!showHud || !HasSample) return;
        var s = Latest;
        float amag = Mathf.Sqrt(s.ax*s.ax + s.ay*s.ay + s.az*s.az);
        GUI.Label(new Rect(10, 10, 500, 20), $"ESP OK | pitch={s.pitch:F1} roll={s.roll:F1} yaw={s.yaw:F1} | |a|={amag:F2}");
    }
}
