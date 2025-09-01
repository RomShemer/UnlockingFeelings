using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class CubeTriggerSender : MonoBehaviour
{
    [SerializeField] private string serverUrl = "http://192.168.1.104:5000/on";

    public void OnTriggerEnter(Collider other)
    {
        Debug.Log("🟢 נגיעה בזיהוי! שולחת בקשה לשרת");
        StartCoroutine(SendToServer());
    }

    public IEnumerator SendToServer()
    {
        UnityWebRequest request = UnityWebRequest.Get(serverUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            Debug.LogError("❌ שגיאה: " + request.error);
        else
            Debug.Log("✅ הצלחה!");
    }
}
