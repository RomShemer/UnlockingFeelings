using UnityEngine;

public class Destroy : MonoBehaviour
{
    public float hp;
    public GameObject Destroyed;
    public GameObject graffitiPlane;
    public float fadeDuration = 2f;
    private Material mat;

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Shoot")
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
                Destroy(graffitiPlane);
                //StartCoroutine(FadeOutGraffiti());
            }


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
    //    //Destroy(graffitiPlane); // ���� �� �������� �����
    //}
    private System.Collections.IEnumerator FadeOutGraffiti()
    {
        Renderer rend = graffitiPlane.GetComponent<Renderer>();
        Material mat = rend.material;

        // ������ ������ ������ Standard
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
        Destroy(graffitiPlane); // ���� �� ��Plane ���� ������ ������
    }
}