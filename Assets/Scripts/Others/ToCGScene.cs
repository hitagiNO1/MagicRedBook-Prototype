using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using System.Collections;

public class ToCGScene : MonoBehaviour
{
    public GameObject InteractiveUI;         // 提示 按E進入
    public string targetSceneName = "NextScene"; // 要切換的場景名稱
    private bool isLoading = false;
    private bool playerInRange = false;
    public PlayableAsset timeline; // Timeline資源參考

    void Start()
    {
        if (InteractiveUI != null)
            InteractiveUI.SetActive(false); // 開始時隱藏提示
    }

    // 將 LoadScene 改為 IEnumerator，並用 StartCoroutine 呼叫
    void Update()
    {
        if (playerInRange && !isLoading && Input.GetKeyDown(KeyCode.E))
        {
            InteractiveUI.SetActive(false);
            StartCoroutine(LoadScene());
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            InteractiveUI.SetActive(true);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            InteractiveUI.SetActive(false);
        }
    }
    // 淡出再切場景
    IEnumerator LoadScene()
    {
        isLoading = true;
        GetComponent<Collider>().enabled = false;

        TimelineManager.Instance.PlayCG(timeline);

        yield return new WaitUntil(() => !TimelineManager.Instance.IsPlaying);

        // 改成 Additive 模式載入 CG
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
        yield return loadOp;

        // 播放 CG 後再卸載
        yield return new WaitUntil(() => !TimelineManager.Instance.IsPlaying);

        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(targetSceneName);
        yield return unloadOp;

        isLoading = false;
        this.enabled = false;
    }

}
