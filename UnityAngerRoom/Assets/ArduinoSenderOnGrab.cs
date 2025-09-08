using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ArduinoSenderOnGrab : MonoBehaviour
{
    [SerializeField] private string serverUrl = "http://192.168.1.104:5000/";

    private XRGrabInteractable grab;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        if (grab == null)
            Debug.LogError("❌ XRGrabInteractable לא נמצא על האובייקט הזה");
    }

    private void OnEnable()
    {
        if (grab != null)
        {
            grab.selectEntered.AddListener(OnGrab);
            grab.selectExited.AddListener(OnRelease);
        }
    }


    private void OnDisable()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnGrab);
            grab.selectExited.RemoveListener(OnRelease);
        }
    }

    private void OnGrab(BaseInteractionEventArgs args)
    {
        Debug.Log("🎮 תפיסה התחילה");
        StartCoroutine(SendToServer("on"));
    }

    private void OnRelease(BaseInteractionEventArgs args)
    {
        Debug.Log("👋 שחרור");
        StartCoroutine(SendToServer("off"));
    }

    private IEnumerator SendToServer(string command)
    {
        string fullUrl = serverUrl + command;
        Debug.Log("📤 שולח בקשה אל: " + fullUrl);

        UnityWebRequest request = UnityWebRequest.Get(fullUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ בקשה נכשלה: " + request.error);
        }
        else
        {
            Debug.Log("✅ בקשה נשלחה בהצלחה!");
        }
    }
}
