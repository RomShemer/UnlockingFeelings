using UnityEngine;
using WebSocketSharp;
using PimDeWitte.UnityMainThreadDispatcher;
using TMPro;

public class HammerWebSocketReceiver : MonoBehaviour
{
    public string websocketUrl = "wss://0d96-2a0d-6fc7-601-dd0f-ac45-ab27-892a-4448.ngrok-free.app/ws/sensor";
    public Transform hammer;
    public TMP_Text debugText;


    private WebSocket ws;

    void Start()
    {
        ws = new WebSocket(websocketUrl);

        ws.OnMessage += (sender, e) =>
        {
            string[] values = e.Data.Split(',');
            if (values.Length == 3 &&
                float.TryParse(values[0], out float pitch) &&
                float.TryParse(values[1], out float roll) &&
                float.TryParse(values[2], out float yaw))
            {
                // חובה להריץ קוד שמעדכן Unity מתוך ה־Main Thread
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    float movementScale = 0.01f;
                    Vector3 delta = new Vector3(pitch, roll, yaw) * movementScale;
                    hammer.position += delta;
                    debugText.text = "Position:" + hammer.position;
                    //hammer.rotation = Quaternion.Euler(pitch, yaw, roll);
                });
            }
        };

        debugText.text = "trying to connect to webServer";
        ws.Connect();
        debugText.text = "connected to webServer";
    }

    void OnApplicationQuit()
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
            debugText.text = "closed connection to webServer";

        }
    }
}
