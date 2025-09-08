using UnityEngine;

public class Destroy : MonoBehaviour
{
    public float hp;
    public GameObject Destroyed;
    public GameObject graffitiPlane;
    public float fadeDuration = 2f;
    private Material mat;

    [SerializeField] private DoorScript.Door door;
    public RoomManager roomManager;

    private void Awake()
    {
       if(roomManager == null)
        {
            //roomManager = FindFirstObjectByType<RoomManager>();
            roomManager = RoomManager.Instance;
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Hammer")
        {
            hp--;
        }
    }

    private void Update()
    {
        if (hp == 0)
        {
            //Instantiate(Destroyed, transform.position, Quaternion.Euler(180, 0, 0));
            Instantiate(Destroyed, transform.position, transform.rotation);

            if (graffitiPlane != null)
            {
                GetComponent<Collider>().enabled = false;
                Destroy(graffitiPlane);
                //StartCoroutine(FadeOutGraffiti());
            }


            roomManager.CompleteMission();
            Debug.Log("comlete mission from destroy script");
            door?.OpenDoor();  // קריאה לפתיחה
            Debug.Log("door open from destroy script");
            Destroy(gameObject);

        }
    }

    //private System.Collections.IEnumerator FadeOut()
    //{
    //    Renderer rend = graffitiPlane.GetComponent<Renderer>();
    //    Material mat = rend.material;
    //    Color startColor = mat.color;
    //    float elapsed = 0f;

    //    while (elapsed < fadeDuration)
    //    {
    //        float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
    //        mat.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
    //        elapsed += Time.deltaTime;
    //        yield return null;
    //    }

    //    mat.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
    //    //Destroy(graffitiPlane); // מוחק את האובייקט בסיום
    //}
    private System.Collections.IEnumerator FadeOutGraffiti()
    {
        Renderer rend = graffitiPlane.GetComponent<Renderer>();
        Material mat = rend.material;

        // הגדרות שקיפות לשיידר Standard
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        Color startColor = mat.color;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            mat.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mat.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        Destroy(graffitiPlane); // מוחק את ה־Plane אחרי שהפייד הסתיים
    }
}