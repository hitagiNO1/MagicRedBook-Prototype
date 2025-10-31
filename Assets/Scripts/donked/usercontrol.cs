using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // 必須加入

public class usercontrol : MonoBehaviour
{
    private List<HoleState> holeStates = new List<HoleState>();
    public bool gameover = false;
    public float timer = 60f;
    private int mouseClickCount = 0;
    private const int requiredClicks = 15;
    private bool gameWon = false;

    public TextMeshProUGUI resault;
    public boxgameflow gameflow;
    public GameObject backbutton;
    private mouseborn mouseborn;

    void Start()
    {
        mouseborn = FindObjectOfType<mouseborn>();
        if (mouseborn == null)
        {
            Debug.LogError("未找到 mouseborn 組件！");
            enabled = false;
            return;
        }

        // 註冊事件
        mouseborn.OnHoleSpawned += RegisterHoleState;
        mouseborn.OnHoleRemoved += UnregisterHoleState;

        // 初始清理
        CleanUpHoleList();
    }

    void OnDestroy()
    {
        // 解除註冊，避免記憶體洩漏
        if (mouseborn != null)
        {
            mouseborn.OnHoleSpawned -= RegisterHoleState;
            mouseborn.OnHoleRemoved -= UnregisterHoleState;
        }
    }

    void Update()
    {
        if (!gameover && !gameWon)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                Debug.Log("時間到！未達標，遊戲結束！");
                gameover = true;
                gamelose();
                gameflow.ShowResult();
            }

            userinput(); // 移到 Update 內，只處理輸入
        }
    }

    public void userinput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Vector3 mousePos = Input.mousePosition;

        // 關鍵修復 1：檢查是否點擊在 Game View 範圍內
        if (mousePos.x < 0 || mousePos.y < 0 ||
            mousePos.x > Screen.width || mousePos.y > Screen.height)
        {
            return; // 點擊在 Unity 編輯器其他區域，忽略
        }

        // 關鍵修復 2：避免點擊到 UI（如按鈕）
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 安全檢查
        if (Camera.main == null)
        {
            Debug.LogWarning("Camera.main 不存在！");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        // 關鍵修復 3：加上距離限制，避免極端 Ray
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, 100f);

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("C4"))
            {
                Debug.Log("點擊到 C4，遊戲結束！");
                HoleState holeState = hit.collider.GetComponent<HoleState>();
                if (holeState != null && mouseborn != null)
                {
                    mouseborn.OnObjectClicked(holeState.Hole);
                }
                gameover = true;
                getC4();
                gameflow.ShowResult();
            }
            else if (hit.collider.CompareTag("mouse"))
            {
                HoleState holeState = hit.collider.GetComponent<HoleState>();
                if (holeState != null && mouseborn != null)
                {
                    Debug.Log("點擊到老鼠：" + hit.collider.gameObject.name);
                    mouseborn.OnObjectClicked(holeState.Hole);

                    // 直接從 list 移除，不依賴 Find
                    holeStates.Remove(holeState);
                    mouseClickCount++;

                    if (mouseClickCount >= requiredClicks)
                    {
                        Debug.Log("成功點擊15次老鼠！遊戲勝利！");
                        gameWon = true;
                        gameWin();
                        backbutton.SetActive(true);
                        gameflow.ShowResult();
                    }
                }
            }
        }
    }

    // 事件回呼：新地洞生成
    private void RegisterHoleState(HoleState state)
    {
        if (state != null && !holeStates.Contains(state))
        {
            holeStates.Add(state);
        }
    }

    // 事件回呼：地洞被移除
    private void UnregisterHoleState(HoleState state)
    {
        holeStates.Remove(state);
    }

    // 定期清理 null 引用
    public void CleanUpHoleList()
    {
        holeStates.RemoveAll(h => h == null || h.gameObject == null);
    }

    public bool IsGameActive()
    {
        return !gameover && !gameWon;
    }

    public void getC4()
    {
        resault.text = "你點錯了!";
    }

    public void gamelose()
    {
        resault.text = "沒在時間內達成目標!";
    }

    public void gameWin()
    {
        resault.text = "達成目標!";
    }

    public void RestartGame()
    {
        gameover = false;
        mouseClickCount = 0;
        timer = 60f;
        gameWon = false;
        holeStates.Clear();
        backbutton.SetActive(false);

        if (mouseborn != null)
        {
            mouseborn.Restart();
        }
    }

    public void BackGame()
    {
        SceneManager.LoadScene("Prologue");
    }
}