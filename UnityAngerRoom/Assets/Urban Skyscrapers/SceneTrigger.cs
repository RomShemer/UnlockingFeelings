using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTrigger : MonoBehaviour
{
    public string sceneToLoad = "darkRoomFearScenes"; // שם הסצנה לטעינה

    [Header("Collect Manager")]
    public CollectPuzzleManager collectManager;

    void Start()
    {
        if (!collectManager) collectManager = FindObjectOfType<CollectPuzzleManager>();
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Player entered trigger, loading scene: " + sceneToLoad);
        if (collectManager.isCollectAllPuzzles())
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
