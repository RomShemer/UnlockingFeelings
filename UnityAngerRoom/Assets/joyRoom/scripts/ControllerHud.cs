using UnityEngine;

public class ControllerHud : MonoBehaviour {
    [Header("Refs")]
    public Transform rightHand;
    public Transform leftHand;
    public Camera mainCam;                        // אופציונלי; אם ריק נאתר לבד
    public EspImuClient esp;                      // אופציונלי: סטטוס IMU
    public BookPickupManager book;                // אופציונלי: סטטוס ספר

    [Header("Display")]
    public bool show = true;
    public int fontSize = 16;
    public Vector2 pos = new Vector2(10, 10);
    public float lineH = 20f;

    void Start() {
        if (!mainCam) mainCam = Camera.main;
    }

    string Vec3Str(Vector3 v) => $"({v.x:F2},{v.y:F2},{v.z:F2})";
    string EulerStr(Vector3 e) => $"({e.x:F0},{e.y:F0},{e.z:F0})";

    void OnGUI() {
        if (!show) return;

        var prev = GUI.skin.label.fontSize;
        GUI.skin.label.fontSize = fontSize;

        float y = pos.y;
        Rect R(float h) => new Rect(pos.x, y += h, 800, lineH);

        // Hands
        bool rOK = rightHand && rightHand.gameObject.activeInHierarchy && float.IsFinite(rightHand.position.sqrMagnitude);
        bool lOK = leftHand  && leftHand .gameObject.activeInHierarchy && float.IsFinite(leftHand .position.sqrMagnitude);

        GUI.Label(R(0), $"--- Controller HUD ---");
        GUI.Label(R(lineH), $"Right: {(rOK?"OK":"X")}  pos={ (rOK?Vec3Str(rightHand.position):"-") }  rot={ (rOK?EulerStr(rightHand.eulerAngles):"-") }");
        GUI.Label(R(lineH),  $"Left : {(lOK?"OK":"X")}  pos={ (lOK?Vec3Str(leftHand .position):"-") }  rot={ (lOK?EulerStr(leftHand .eulerAngles):"-") }");

        // ESP
        if (esp) {
            string s = esp.HasSample ? "ESP: OK" : "ESP: NO DATA";
            GUI.Label(R(lineH), s + (esp.HasSample ? $" | pitch={esp.Latest.pitch:F1} roll={esp.Latest.roll:F1} | |a|={(new Vector3(esp.Latest.ax,esp.Latest.ay,esp.Latest.az)).magnitude:F2}" : ""));
        }

        // Book
        if (book) {
            GUI.Label(R(lineH), $"Book: state={book.name} {(book.enabled ? "(enabled)" : "(disabled)")}");
        }

        GUI.skin.label.fontSize = prev;
    }
}
