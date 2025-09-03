using UnityEditor;
using UnityEngine;

public class QuadsFromTextures : EditorWindow
{
    public Material material;        // המטריאל הדו-צדדי (UF/TwoSidedCutout)
    public bool makeCrossed = true;  // שתי חזיתות בצורת X
    public bool saveAsPrefabs = true;
    public string prefabFolder = "Assets/prefabs";
    public float scale = 1f;

    [MenuItem("Tools/UF/Create Quads From Selected Textures")]
    static void Open() => GetWindow<QuadsFromTextures>("Quads From Textures");

    void OnGUI() {
        material = (Material)EditorGUILayout.ObjectField("Material", material, typeof(Material), false);
        makeCrossed = EditorGUILayout.Toggle("Make Crossed (X)", makeCrossed);
        saveAsPrefabs = EditorGUILayout.Toggle("Save As Prefabs", saveAsPrefabs);
        prefabFolder = EditorGUILayout.TextField("Prefab Folder", prefabFolder);
        scale = EditorGUILayout.Slider("Scale", scale, 0.05f, 5f);
        if (GUILayout.Button("Create From Selected Textures")) Create();
    }

    void Create() {
        var sel = Selection.objects;
        if (sel == null || sel.Length == 0) { EditorUtility.DisplayDialog("No Selection","סמן טקסטורות PNG ב-Project","אוקיי"); return; }
        if (!material) { EditorUtility.DisplayDialog("Material","בחר מטריאל דו-צדדי UF/TwoSidedCutout","אוקיי"); return; }

        if (saveAsPrefabs && !AssetDatabase.IsValidFolder(prefabFolder)) {
            System.IO.Directory.CreateDirectory(prefabFolder); AssetDatabase.Refresh();
        }

        var parent = new GameObject("Generated_Quads");
        Undo.RegisterCreatedObjectUndo(parent,"Create Quads Parent");

        foreach (var o in sel) {
            var tex = o as Texture2D; if (!tex) continue;

            var root = new GameObject("Quad_" + tex.name);
            root.transform.localScale = Vector3.one * scale;
            root.transform.SetParent(parent.transform, true);

            GameObject MakeFace(Quaternion rot) {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.transform.SetParent(root.transform,false);
                q.transform.localRotation = rot;
                var mr = q.GetComponent<MeshRenderer>();
                mr.sharedMaterial = new Material(material);
                // URP + Built-in:
                mr.sharedMaterial.SetTexture("_BaseMap", tex);
                mr.sharedMaterial.SetTexture("_MainTex", tex);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                return q;
            }

            MakeFace(Quaternion.identity);
            if (makeCrossed) MakeFace(Quaternion.Euler(0,90,0));

            if (saveAsPrefabs) {
                var path = $"{prefabFolder}/Quad_{tex.name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
        }

        EditorUtility.DisplayDialog("Done","נוצרו קוואדים לכל הטקסטורות שנבחרו.","יש!");
        EditorGUIUtility.PingObject(parent);
    }
}
