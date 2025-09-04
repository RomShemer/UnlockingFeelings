using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

[Serializable]
public class YprMsg { public float yaw, pitch, roll; } // במעלות

public class BookEulerReceiver : MonoBehaviour
{
    public int listenPort = 4211;
    public Transform virtualBook;

    UdpClient client;
    YprMsg last = new YprMsg();
    bool hasCalib = false;
    Vector3 calibEuler = Vector3.zero; // נשמור YPR של נקודת האפס

    void Start()
    {
        client = new UdpClient(listenPort);
        client.BeginReceive(OnRx, null);
        Debug.Log($"UDP listening on {listenPort}");
    }

    void OnRx(IAsyncResult ar)
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, listenPort);
        byte[] data = client.EndReceive(ar, ref ep);
        string s = Encoding.UTF8.GetString(data);

        try { last = JsonUtility.FromJson<YprMsg>(s); }
        catch { /* JSON לא תקין */ }

        client.BeginReceive(OnRx, null);
    }

    void Update()
    {
        if (!virtualBook) return;

        if (Input.GetKeyDown(KeyCode.C))
        {
            calibEuler = new Vector3(last.pitch, last.yaw, last.roll);
            hasCalib = true;
        }

        Vector3 e = new Vector3(last.pitch, last.yaw, last.roll);
        if (hasCalib) e -= calibEuler;

        Quaternion target = Quaternion.Euler(e);
        virtualBook.localRotation = Quaternion.Slerp(
            virtualBook.localRotation, target, 20f * Time.deltaTime);
    }

    void OnApplicationQuit() { client?.Close(); }
}
