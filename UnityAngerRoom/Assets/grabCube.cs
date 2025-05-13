using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CubeGrabSender : MonoBehaviour
{
    [SerializeField] private string serverUrl = "https://dd88-62-90-179-254.ngrok-free.app/on";
    [SerializeField] private string serverUrlExit = "https://dd88-62-90-179-254.ngrok-free.app/off";
    //[SerializeField] private TMP_Text consoleText;

    private XRGrabInteractable grab;
    private Rigidbody rb;

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
        grab.selectExited.AddListener(OnRelease); // 🎯 הוספנו גם שחרור
        LogToConsole("✅ Awake – מאזין נוסף");
    }

    public void OnGrab(SelectEnterEventArgs args)
    {
        LogToConsole("🤲 תפסנו את הקובייה!");
        StartCoroutine(SendToServer());

        // בזמן תפיסה – נעצור את הפיזיקה
        rb.isKinematic = true;
    }

    public void OnRelease(SelectExitEventArgs args)
    {
        LogToConsole("👋 שחררנו את הקובייה, מפעילים פיזיקה");
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.WakeUp(); // להפעיל פיזיקה מיד
        LogToConsole("📡 שולחת בקשה לשרת...");
        StartCoroutine(SendToServerExit());
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

    private IEnumerator SendToServerExit()
    {
        LogToConsole("📡 שולחת בקשה לשרת...");
        UnityWebRequest request = UnityWebRequest.Get(serverUrlExit);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            LogToConsole("❌ שגיאה: " + request.error);
        else
            LogToConsole("✅ הצלחה!");
    }

    private void LogToConsole(string message)
    {
        Debug.Log(message);
        //if (consoleText != null)
        //    consoleText.text = message;
    }
}
