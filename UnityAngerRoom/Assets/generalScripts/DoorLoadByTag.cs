using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class DoorLoadByTag : MonoBehaviour
{
    [Header("Who can open")]
    [Tooltip("������ ����� ��� ����� (���� ������/����������).")]
    public LayerMask whoCanOpen = ~0;

    [Tooltip("�� true ����� �-Trigger; ���� �-Collision ����.")]
    public bool useTrigger = true;

    [Header("Scene names per door tag")]
    public string joyScene = "JoyScene";
    public string angerScene = "AngerScene";
    public string fearScene = "FearScene";
    public string sadnessScene = "SadnessScene";

    [Header("If SceneLoader exists")]
    [Tooltip("�� SceneLoader.useFixedOrder=true, ������ �� ���� ����� ���.")]
    public bool startFixedSequenceFromThis = true;

    SceneLoader loader;

    void Reset()
    {
        // ����� ����: ��� �-Trigger �� Rigidbody �����
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;

        if (!TryGetComponent<Rigidbody>(out var rb))
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void Awake()
    {
        loader = FindFirstObjectByType<SceneLoader>();
        Debug.Log("DoorLoadByTag: Found SceneLoader: " + (loader ? loader.name : "none"));
    }

    // --- Triggers ---
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[TriggerProbe] ENTER by {other.name} (layer={LayerMask.LayerToName(other.gameObject.layer)}) on {name}");

        if (!useTrigger)
        {
            Debug.Log("  Ignored (not a trigger).");
            return;
        }
        if (!IsAllowed(other.gameObject.layer))
        {
            Debug.Log("  Ignored (layer not allowed).");
            return;
        }

        Debug.Log("  Accepted, loading scene...");
        LoadByMyTag();
    }

    // --- Collisions (�� ����� ��� Trigger) ---
    void OnCollisionEnter(Collision collision)
    {
        if (useTrigger) return;
        if (!IsAllowed(collision.collider.gameObject.layer)) return;
        LoadByMyTag();
    }

    bool IsAllowed(int layer) => (whoCanOpen.value & (1 << layer)) != 0;

    void LoadByMyTag()
    {
        if (!RoomRunManager.AreDoorsEnabled) return;

        Debug.Log($"DoorLoadByTag: Loading scene for tag '{gameObject.tag}'...");
        string scene = SceneForTag(gameObject.tag);

        if (string.IsNullOrEmpty(scene))
        {
            Debug.LogWarning($"DoorLoadByTag: Unrecognized tag '{gameObject.tag}'.");
            return;
        }

        // �� �� SceneLoader � ����� ��, ���� ��� ���� �� ����
        if (loader)
        {
            //if (loader.useFixedOrder && startFixedSequenceFromThis)
            //    loader.StartSequenceFrom(scene);
            //else

            bool ok = loader.LoadIfAllowed(scene);
            if (ok)
            {
                Debug.Log("Using SceneLoader to load scene: " + scene);
                loader.LoadIfAllowed(scene);
            } 
            //todo: ����� ������
        }
        else
        {
            Debug.Log("No SceneLoader found, loading scene directly: " + scene);
            // ����� ����� (��� ����/���)
            SceneManager.LoadScene(scene);
        }
    }

    string SceneForTag(string doorTag)
    {
        switch (doorTag)
        {
            case "joyDoor": return "JoyScene 1";
            case "angerDoor": return "AngerScene";
            case "fearDoor": return "FearScene";
            case "sadnessDoor": return "SadnessScene";
            default: return null;
        }
    }
}
