using UnityEditor;
using UnityEngine;

public class SpawnQuadGrid : EditorWindow
{
    public GameObject quadPrefab;
    public int rows = 3, cols = 8;
    public float spacingX = 0.6f, spacingZ = 0.6f;
    public bool randomRotate = true;
    public float jitter = 0.15f;          // רעידה קטנה למראה טבעי
    public LayerMask groundMask;
    public float rayHeight = 5f;

    [MenuItem("Tools/UF/Spawn Quad Grid")]
    static void Open() => GetWindow<SpawnQuadGrid>("Spawn Quad Grid");

    void OnGUI() {
        quadPrefab = (GameObject)EditorGUILayout.ObjectField("Quad Prefab", quadPrefab, typeof(GameObject), false);
        rows = EditorGUILayout.IntField("Rows", rows);
        cols = EditorGUILayout.IntField("Cols", cols);
        spacingX = EditorGUILayout.FloatField("Spacing X", spacingX);
        spacingZ = EditorGUILayout.FloatField("Spacing Z", spacingZ);
        randomRotate = EditorGUILayout.Toggle("Random Y Rotation", randomRotate);
        jitter = EditorGUILayout.Slider("Position Jitter", jitter, 0f, 0.5f);
        groundMask = LayerMaskField("Ground Mask", groundMask);
        rayHeight = EditorGUILayout.FloatField("Ray Height", rayHeight);

        if (GUILayout.Button("Spawn At Scene View Pivot")) Spawn();
    }

    static LayerMask LayerMaskField(string label, LayerMask selected) {
        var layers = UnityEditorInternal.InternalEditorUtility.layers;
        int mask = 0; for (int i=0;i<layers.Length;i++) if (((1<<LayerMask.NameToLayer(layers[i])) & selected.value) != 0) mask |= (1<<i);
        mask = EditorGUILayout.MaskField(label, mask, layers);
        int newMask = 0; for (int i=0;i<layers.Length;i++) if ((mask & (1<<i)) != 0) newMask |= (1<<LayerMask.NameToLayer(layers[i]));
        selected.value = newMask; return selected;
    }

    void Spawn() {
        if (!quadPrefab) { EditorUtility.DisplayDialog("Pick Prefab","בחר פריפאב קוואד (מהשלב הקודם).","סגור"); return; }

        var root = new GameObject("QuadGrid");
        Undo.RegisterCreatedObjectUndo(root,"Create QuadGrid");

        // מרכז סביב נקודת המצלמה של ה-Scene View
        var origin = SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        float ox = origin.x - (cols-1)*spacingX*0.5f;
        float oz = origin.z - (rows-1)*spacingZ*0.5f;

        for (int r=0;r<rows;r++)
        for (int c=0;c<cols;c++) {
            var p = (GameObject)PrefabUtility.InstantiatePrefab(quadPrefab);
            var pos = new Vector3(ox + c*spacingX, origin.y, oz + r*spacingZ);
            pos += new Vector3(Random.Range(-jitter,jitter), 0, Random.Range(-jitter,jitter));

            // הצמדה לקרקע (Plane/קרקע בלייר Ground)
            var rayStart = pos + Vector3.up*rayHeight;
            if (Physics.Raycast(rayStart, Vector3.down, out var hit, rayHeight*2f, groundMask))
                pos.y = hit.point.y;

            p.transform.position = pos;
            if (randomRotate) p.transform.rotation = Quaternion.Euler(0, Random.Range(0f,360f), 0);
            p.transform.SetParent(root.transform);
        }

        EditorGUIUtility.PingObject(root);
    }
}
