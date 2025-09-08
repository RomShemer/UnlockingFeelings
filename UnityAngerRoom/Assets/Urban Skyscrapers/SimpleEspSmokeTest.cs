using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class SimpleEspSmokeTest : MonoBehaviour
{
    public string espBaseUrl = "http://10.100.102.5";
    public bool blinkLoop = false;
    public float blinkInterval = 1f;
    public int timeoutSec = 3;

    void Start() { StartCoroutine(Run()); }

    IEnumerator Run()
    {
        yield return Send("/status");
        yield return Send("/led/on"); yield return new WaitForSeconds(1f);
        yield return Send("/led/off");

        if (blinkLoop) StartCoroutine(Blink());
    }

    IEnumerator Blink()
    {
        while (true)
        {
            yield return Send("/led/on"); yield return new WaitForSeconds(blinkInterval);
            yield return Send("/led/off"); yield return new WaitForSeconds(blinkInterval);
        }
    }

    IEnumerator Send(string path)
    {
        using (var r = UnityWebRequest.Get(espBaseUrl + path))
        {
            r.SetRequestHeader("ngrok-skip-browser-warning", "true");
            r.timeout = Mathf.Max(1, timeoutSec);
            yield return r.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            if (r.result != UnityWebRequest.Result.Success)
#else
            if (r.isNetworkError || r.isHttpError)
#endif
                Debug.LogWarning($"[ESP] FAIL {path}: {r.error}");
            else
                Debug.Log($"[ESP] OK {path}: {r.downloadHandler.text}");
        }
    }
}
