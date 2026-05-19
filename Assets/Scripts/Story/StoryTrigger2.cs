using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class StoryTrigger2 : MonoBehaviour
{
    bool scope = false;
    private bool isTriggered = false;
    private bool isTreeDeleted = false; // 新增：用來記錄 Deltree 是否已經被刪除過

    public float sec = 2f;              // 刪除時間
    public GameObject Deltree;
    [Header("條件：這些劇情都要播完")]
    public List<GameScene> requiredScenes;

    [Header("條件 未完成時 劇情")]
    public StoryScene sceneA;

    [Header("條件 完成時 劇情")]
    public StoryScene sceneB;


    [HideInInspector]
    public GameController Gamescript;
    [Header("劇情進場驚嘆號動畫設定")]
    [HideInInspector] // 同樣隱藏，不用手動拖曳
    public Animator introAnimator;
    void Start()
    {
        // 1. 自動尋找 GameController (因為你有寫單例 Instance，直接抓就好！)
        Gamescript = GameController.Instance;

        // 2. 自動尋找驚嘆號物件並取得 Animator 
        GameObject introObj = GameObject.Find("TriggerStory");
        if (introObj != null)
        {
            introAnimator = introObj.GetComponent<Animator>();
        }
        else
        {
            Debug.LogWarning("找不到驚嘆號物件，請確認 GameObject.Find 裡的名稱是否正確。");
        }
    }
    void Update()
    {
        // --------------------------------------------------------
        // 功能 1：隨時偵測條件，一旦達成直接刪除 Deltree (不看範圍)
        // --------------------------------------------------------
        if (!isTreeDeleted && AllScenesPlayed())
        {
            if (Deltree != null)
            {
                Destroy(Deltree);
            }
            isTreeDeleted = true; // 標記為已刪除，避免 Update 每幀重複執行
        }

        // --------------------------------------------------------
        // 功能 2：偵測範圍，玩家進入才觸發劇情
        // --------------------------------------------------------
        if (scope && !isTriggered)
        {
            if (AllScenesPlayed())
            {
                // 所有條件劇情播完 → 播放 B
                GameController.Instance.PlayScene(sceneB);

                // 觸發完成後，一段時間刪除自己 (這個觸發器)
                StartCoroutine(Delet(sec));
            }
            else
            {
                // 尚未全部播完 → 播放 A
                GameController.Instance.PlayScene(sceneA);
            }

            if (introAnimator != null)
            {
                introAnimator.SetTrigger("PlayIntro");
            }
            isTriggered = true; // 確保只執行一次
        }
    }

    /// <summary>
    /// 檢查所有指定劇情是否都播完
    /// </summary>
    private bool AllScenesPlayed()
    {
        foreach (GameScene scene in requiredScenes)
        {
            bool played;
            // 沒有紀錄，或紀錄為 false → 表示沒播完
            if (!GameController.Instance.ScenePlayedState.TryGetValue(scene, out played) || !played)
            {
                return false;
            }
        }
        return true;
    }

    private IEnumerator Delet(float sec)
    {
        yield return new WaitForSeconds(sec);
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)//檢測角色是否進入對話功能範圍
    {
        if (other.gameObject.tag == "Player")
        {
            scope = true;
        }
    }

    void OnTriggerExit(Collider other)//離開範圍
    {
        if (other.gameObject.tag == "Player")
        {
            scope = false;
        }
    }
}