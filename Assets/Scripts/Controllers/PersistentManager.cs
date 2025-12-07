using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentManager : MonoBehaviour
{
    // Singleton確保只有一個管理器實例
    public static PersistentManager Instance { get; private set; }

    // 拖拽任意物件到這裡，它們會被標記為持久化
    [Header("要持久化的物件（拖拽進來）")]
    [SerializeField] private List<GameObject> objectsToPersist = new List<GameObject>();

    // 容器Transform，用來統一管理持久物件的父級（可選，避免層級混亂）
    [Header("持久物件容器（可選）")]
    [SerializeField] private Transform persistentContainer;

    private void Awake()
    {
        // Singleton邏輯：如果已存在其他實例，銷毀此實例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 標記管理器本身不被銷毀
        DontDestroyOnLoad(gameObject);

        // 如果沒指定容器，自動創建一個
        if (persistentContainer == null)
        {
            GameObject container = new GameObject("PersistentObjects");
            persistentContainer = container.transform;
            persistentContainer.SetParent(transform);
            DontDestroyOnLoad(container);
        }

        // 處理每個要持久化的物件
        foreach (GameObject obj in objectsToPersist)
        {
            if (obj != null)
            {
                // 檢查是否已標記（避免重複）
                if (!obj.CompareTag("Persistent")) // 我們用Tag來標記
                {
                    obj.tag = "Persistent"; // 簡單標記
                    DontDestroyOnLoad(obj);
                    // 移到容器下（可選，保持層級整潔）
                    obj.transform.SetParent(persistentContainer);
                }
            }
        }

        Debug.Log("PersistentManager 初始化完成！已持久化 " + objectsToPersist.Count + " 個物件。");
    }

    // 可選：動態添加物件（運行時）
    public void AddPersistentObject(GameObject obj)
    {
        if (obj != null && !objectsToPersist.Contains(obj))
        {
            objectsToPersist.Add(obj);
            obj.tag = "Persistent";
            DontDestroyOnLoad(obj);
            obj.transform.SetParent(persistentContainer);
            Debug.Log("動態添加持久物件: " + obj.name);
        }
    }

    // 可選：移除持久化（如果需要）
    public void RemovePersistentObject(GameObject obj)
    {
        if (objectsToPersist.Contains(obj))
        {
            objectsToPersist.Remove(obj);
            // 注意：無法Undo DontDestroyOnLoad，只能銷毀物件
            Destroy(obj);
            Debug.Log("移除持久物件: " + obj.name);
        }
    }

    // 場景切換事件鉤子（可選，整合additive loading）
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 新場景加載後，檢查並重新parent持久物件（如果additive模式）
        if (mode == LoadSceneMode.Additive)
        {
            foreach (GameObject obj in objectsToPersist)
            {
                if (obj != null && obj.scene == scene)
                {
                    obj.transform.SetParent(persistentContainer);
                }
            }
        }
        Debug.Log("場景加載完成: " + scene.name + "。持久物件狀態已保留。");
    }
}