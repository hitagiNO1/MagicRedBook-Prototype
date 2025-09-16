using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class ScenePortal : MonoBehaviour
{
    public GameObject InteractiveUI;         // 提示 按E進入
    public string targetSceneName = "NextScene"; // 要切換的場景名稱
    public Image fadeImage;                  // 黑幕
    public float fadeDuration = 1f;          // 淡入淡出時間
    private bool isLoading = false;
    private bool playerInRange = false;

    private Transform playerTransform; // 儲存玩家 Transform
    public locationstore locationstore;

    void Start()
    {
        if (InteractiveUI != null)
            InteractiveUI.SetActive(false); // 開始時隱藏提示

        if (fadeImage != null)
            fadeImage.gameObject.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && !isLoading && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(FadeAndLoadScene());
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            InteractiveUI.SetActive(true);
            playerTransform = other.transform; // 儲存玩家位置
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            InteractiveUI.SetActive(false);
            playerTransform = null;
        }
    }
    // 淡出再切場景
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

        locationstore.Instance.SavePlayerPosition(playerTransform.position); // 儲存位置
        SceneManager.LoadScene(targetSceneName);
    }
}
