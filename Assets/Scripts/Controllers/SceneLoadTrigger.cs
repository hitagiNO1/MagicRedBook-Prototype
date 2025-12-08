using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoadTrigger : MonoBehaviour
{
    
    [Header("Settings")]
    public string miniGameSceneName; // 小遊戲場景的名稱

    public GameObject InteractiveUI;         // 提示 按E進入
    public Image fadeImage;                  // 黑幕
    public float fadeDuration = 1f;          // 淡入淡出時間
    private bool isLoading = false;
    private bool playerInRange = false;



    void Start()
    {
        if (InteractiveUI != null)
            InteractiveUI.SetActive(false); // 開始時隱藏提示

        if (fadeImage != null)
            fadeImage.gameObject.SetActive(false);
    }

    public void Update()
    {
        if (playerInRange && !isLoading && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(FadeAndLoadScene());
            
            StartCoroutine(SwitchToMiniGame());
            
        }
    }

    IEnumerator FadeAndLoadScene()
    {
        if (fadeImage != null)
        {
            isLoading = true;
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0, 0, 0, 0); // 確保開始為透明

            float elapsed = 0f;
            Color c = fadeImage.color;
            // 淡出黑幕
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                fadeImage.color = new Color(c.r, c.g, c.b, alpha);
                yield return null;
            }
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

    private IEnumerator SwitchToMiniGame()
    {
        // 1. 取得當前的主場景 (觸發器所在的場景)
        Scene mainScene = gameObject.scene;

        // 2. 使用 Additive 模式載入小遊戲，這樣主場景才不會被移除
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(miniGameSceneName, LoadSceneMode.Additive);

        // 等待載入完成
        while (!loadOp.isDone)
        {
            yield return null;
        }

        // 3. 載入完成後，將主場景的所有根物件隱藏
        // 這會讓主場景「暫停」並「消失」，但記憶體中還在
        if (mainScene.IsValid())
        {
            GameObject[] rootObjects = mainScene.GetRootGameObjects();
            foreach (GameObject obj in rootObjects)
            {
                obj.SetActive(false);
            }
        }

        // 4. 設定小遊戲為「主動場景」，這樣新生成的物件會預設放在小遊戲場景中 (可選)
        Scene miniGameScene = SceneManager.GetSceneByName(miniGameSceneName);
        if (miniGameScene.IsValid())
        {
            SceneManager.SetActiveScene(miniGameScene);
        }
    }
}