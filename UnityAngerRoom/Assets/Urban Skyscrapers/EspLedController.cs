// EspHttpOnGrab.cs  — פשוט: שולח HTTP על תפיסה/שחרור, לא משנה פיזיקה
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class EspHttpOnGrab : MonoBehaviour
{
    [Header("ESP8266")]
    [SerializeField] string espBaseUrl = "http://10.100.102.5";
    [SerializeField] string onGrabPath = "/led/on";
    [SerializeField] string onReleasePath = "/led/off";

    [Header("Behavior")]
    [Tooltip("אם מסומן – שולח רק טוגל בעת תפיסה (דורש /led/toggle בלוח)")]
    [SerializeField] bool toggleOnGrabOnly = false;

    [Header("Network")]
    [SerializeField] int requestTimeoutSeconds = 3;

    XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
        Debug.Log("EspHttpOnGrab ready");
    }

    void OnDestroy()
    {
        if (!grab) return;
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs _)
    {
        if (toggleOnGrabOnly) StartCoroutine(Send("/led/toggle"));
        else StartCoroutine(Send(onGrabPath));
    }

    void OnRelease(SelectExitEventArgs _)
    {
        if (!toggleOnGrabOnly) StartCoroutine(Send(onReleasePath));
    }

    IEnumerator Send(string path)
    {
        if (string.IsNullOrWhiteSpace(espBaseUrl)) yield break;
        using (var req = UnityWebRequest.Get(espBaseUrl + path))
        {
            req.SetRequestHeader("ngrok-skip-browser-warning", "true");
            req.timeout = Mathf.Max(1, requestTimeoutSeconds);
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool ok = (req.result == UnityWebRequest.Result.Success);
#else
            bool ok = !req.isNetworkError && !req.isHttpError;
#endif
            if (!ok) Debug.LogWarning($"❌ {path} → {req.error}");
            else Debug.Log($"✅ {path} → {req.downloadHandler.text}");
        }
    }
}
