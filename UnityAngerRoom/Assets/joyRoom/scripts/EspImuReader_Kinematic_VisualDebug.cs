using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class EspImuReader_Kinematic_VisualDebug : MonoBehaviour
{
    [Header("Network")]
    public string espUrl = "http://10.0.0.2/sensor";
    [Range(0.01f, 1f)] public float updateInterval = 0.05f; // ~20Hz
    public bool enableSensor = true;   // אפשר להדליק/לכבות בזמן ריצה

    [Header("Target (Book)")]
    public Transform book;
    public bool autoFindBook = true;
    public string bookTag = "Book";

    [Header("Physics (Method 1)")]
    public bool forceKinematic = true;

    [Header("Rotation Control")]
    [Range(0f, 1f)] public float rotationSensitivity = 0.6f; // גבוה לראות תנועה

    [Header("Keyboard Simulator")]
    public bool keyboardSimulator = true; // חיצים + Q/E

    [Header("Visual Debug")]
    public bool spawnDebugMarker = true;
    public float markerHeight = 0.25f;

    // --- internals ---
    private Coroutine fetchCoroutine;
    private bool sensorRunning = false;
    private Rigidbody rb;

    // marker + status text
    private Transform marker;
    private Renderer markerRenderer;
    private Color baseColor = Color.green;
    private float flashTimer = 0f;
    private Color flashColor = Color.clear;
    private TextMesh statusText;

    // ping
    private string espHost = "";
    private Ping pingObj;
    private float nextPingAt = 0f;
    private int lastPingMs = -1;

    // request stats
    private int reqCount = 0;
    private string lastResult = "N/A";

    [Serializable]
    public class SensorData { public float pitch, roll, yaw; public float ax, ay, az, gx, gy, gz; }

    void Start()
    {
        // book auto-find
        if (book == null && autoFindBook)
        {
            GameObject byTag = null;
            try { byTag = GameObject.FindWithTag(bookTag); } catch { }
            if (byTag != null) book = byTag.transform;
            if (book == null)
            {
                foreach (var t in GameObject.FindObjectsOfType<Transform>())
                {
                    string n = t.name.ToLower();
                    if (n.Contains("book") || n.Contains("buch")) { book = t; break; }
                }
            }
        }
        if (book == null) { Debug.LogError("[ESP] Book not assigned/found."); enabled = false; return; }

        // physics off
        rb = book.GetComponent<Rigidbody>();
        if (rb != null && forceKinematic) { rb.isKinematic = true; rb.useGravity = false; }

        // marker + status text (3D, לא צריך TMP)
        if (spawnDebugMarker)
        {
            marker = new GameObject("DEBUG_MARKER").transform;
            marker.SetParent(book, false);
            marker.localPosition = new Vector3(0, markerHeight, 0);

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(marker, false);
            cube.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            markerRenderer = cube.GetComponent<Renderer>();
            if (markerRenderer != null) markerRenderer.material.color = baseColor;
            var col = cube.GetComponent<Collider>(); if (col) Destroy(col);

            // status 3D text
            var textGO = new GameObject("STATUS_TEXT");
            textGO.transform.SetParent(marker, false);
            textGO.transform.localPosition = new Vector3(0, 0.08f, 0);
            statusText = textGO.AddComponent<TextMesh>();
            statusText.fontSize = 32;
            statusText.characterSize = 0.01f;
            statusText.anchor = TextAnchor.UpperLeft;
            statusText.alignment = TextAlignment.Left;
            statusText.color = Color.white;
        }

        // parse host for ping
        try { espHost = new Uri(espUrl).Host; } catch { espHost = ""; }

        // start/stop by flag
        ApplyEnableSensor(enableSensor, immediate: true);
        UpdateStatusText();
    }

    void Update()
    {
        // runtime toggle support
        if (enableSensor != sensorRunning) ApplyEnableSensor(enableSensor, immediate: false);

        // keyboard simulator
        if (keyboardSimulator && book != null)
        {
            float spd = 120f * Time.deltaTime;
            float dp = (Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
            float dy = (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            float dr = (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0);
            if (dp != 0 || dy != 0 || dr != 0)
            {
                var e = book.localRotation.eulerAngles;
                e.x = NormalizeAngle(e.x + dp * spd);
                e.y = NormalizeAngle(e.y + dy * spd);
                e.z = NormalizeAngle(e.z + dr * spd);
                book.localRotation = Quaternion.Euler(e);
            }
        }

        // marker bob + flash color
        if (marker != null)
        {
            float bob = Mathf.Sin(Time.time * 6f) * 0.01f;
            marker.localPosition = new Vector3(0, markerHeight + bob, 0);
            marker.localRotation = Quaternion.Euler(0, Time.time * 90f, 0);

            if (markerRenderer != null)
            {
                if (flashTimer > 0f)
                {
                    flashTimer -= Time.deltaTime;
                    markerRenderer.material.color = Color.Lerp(baseColor, flashColor, Mathf.Clamp01(flashTimer * 6f));
                }
                else markerRenderer.material.color = baseColor;
            }
        }

        // ping every 2s
        if (!string.IsNullOrEmpty(espHost))
        {
            if (pingObj == null && Time.time >= nextPingAt)
            {
                pingObj = new Ping(espHost);
                nextPingAt = Time.time + 2f;
            }
            else if (pingObj != null && pingObj.isDone)
            {
                lastPingMs = pingObj.time; // -1 when failed
                pingObj = null;
                UpdateStatusText();
            }
        }
    }

    void ApplyEnableSensor(bool on, bool immediate)
    {
        if (on == sensorRunning) return;
        sensorRunning = on;

        if (on)
        {
            if (fetchCoroutine == null) fetchCoroutine = StartCoroutine(FetchSensorLoop());
        }
        else
        {
            if (fetchCoroutine != null) { StopCoroutine(fetchCoroutine); fetchCoroutine = null; }
        }

        if (immediate) UpdateStatusText();
    }

    IEnumerator FetchSensorLoop()
    {
        var wait = new WaitForSeconds(updateInterval);
        while (sensorRunning)
        {
            yield return GetSensorData();
            yield return wait;
        }
    }

    IEnumerator GetSensorData()
    {
        using (var www = UnityWebRequest.Get(espUrl))
        {
            www.timeout = 3;
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                lastResult = "ERR(" + www.error + ")";
                Flash(Color.red);
                UpdateStatusText();
                yield break;
            }

            string json = www.downloadHandler.text;
            SensorData data = null;
            try { data = JsonUtility.FromJson<SensorData>(json); } catch { data = null; }

            if (data == null)
            {
                lastResult = "ERR(parse)";
                Flash(Color.red);
                UpdateStatusText();
                yield break;
            }

            reqCount++;
            lastResult = "OK";
            Flash(new Color(0.2f, 0.6f, 1f)); // blue
            UpdateStatusText();

            ApplyRotationFromSensor(data);
        }
    }

    void ApplyRotationFromSensor(SensorData data)
    {
        // mapping: X <- -roll, Y <- yaw, Z <- -pitch
        float x = NormalizeAngle(-data.roll);
        float y = NormalizeAngle( data.yaw );
        float z = NormalizeAngle(-data.pitch);
        var target = Quaternion.Euler(x, y, z);
        book.localRotation = Quaternion.Slerp(book.localRotation, target, rotationSensitivity);
    }

    void UpdateStatusText()
    {
        if (statusText == null) return;
        string pingStr = (lastPingMs < 0) ? "Ping: fail" : (lastPingMs == 0 ? "Ping: ..." : $"Ping: {lastPingMs} ms");
        statusText.text =
            $"Sensor: {(sensorRunning ? "ON" : "OFF")}\n" +
            $"Req#: {reqCount}  Last: {lastResult}\n" +
            $"{pingStr}\n" +
            $"{espUrl}";
    }

    void Flash(Color c) { flashColor = c; flashTimer = 0.2f; }

    static float NormalizeAngle(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f; else if (a < -180f) a += 360f;
        return a;
    }
}
