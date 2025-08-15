using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class usercontrol : MonoBehaviour
{
    private List<mouseset> mouseset;
    public bool gameover = false;
    public float timer = 60f; // 1分鐘倒數計時器 (秒)
    private int mouseClickCount = 0; // 記錄點擊老鼠的次數
    private const int requiredClicks = 15; // 需要點擊15次老鼠
    private bool gameWon = false; // 記錄遊戲是否成功
    public TextMeshProUGUI resault;
    public boxgameflow gameflow;
    public GameObject backbutton;

    // Start 在第一幀之前調用
    void Start()
    {
       
    }

    // Update 每幀調用一次
    void Update()
    {
        if (!gameover && !gameWon)
        {
            // 更新計時器
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                Debug.Log("時間到！未達標，遊戲結束！");
                gameover = true;
                gamelose();
                gameflow.ShowResult();
            }

            // 每幀檢查是否有新的老鼠物件需要加入列表
            GetMouse();
            userinput();
        }
    }

    public void userinput()
    {
        if (Input.GetMouseButtonDown(0)) // 左鍵點擊
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if (hit.collider != null)
            {
                // 檢查是否點擊到標籤為 "C4" 的物件
                if (hit.collider.CompareTag("C4"))
                {
                    Debug.Log("點擊到 C4，遊戲結束！");
                    gameover = true;
                    getC4();
                    gameflow.ShowResult();
                }
                // 檢查點擊的物件是否有 mouseset 組件
                else if (hit.collider.CompareTag("mouse"))
                {
                    mouseset mouse = hit.collider.GetComponent<mouseset>();
                    if (mouse != null)
                    {
                        // 點擊到老鼠物件
                        Debug.Log("點擊到老鼠：" + hit.collider.gameObject.name);
                        // 從列表中移除並銷毀物件
                        mouseset.Remove(mouse);
                        Destroy(hit.collider.gameObject);
                        // 增加點擊計數
                        mouseClickCount++;
                        // 檢查是否達成15次點擊
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
                else
                {
                    Debug.Log("點擊到物件，但不是老鼠或 C4：" + hit.collider.gameObject.name);
                }
            }
            else
            {
                Debug.Log("未點擊到任何物件");
            }
        }
    }

    public void GetMouse()
    {
        // 如果遊戲結束或勝利，停止生成老鼠
        if (gameover || gameWon)
        {
            return;
        }

        // 查找所有標籤為 "mouse" 的物件
        GameObject[] ms = GameObject.FindGameObjectsWithTag("mouse");
        foreach (var o in ms)
        {
            mouseset mouse = o.GetComponent<mouseset>();
            if (mouse != null && !mouseset.Contains(mouse))
            {
                mouseset.Add(mouse);
            }
        }
    }

    // 移除無效的老鼠物件（例如已被銷毀的）
    public void CleanUpMouseList()
    {
        mouseset.RemoveAll(mouse => mouse == null || mouse.gameObject == null);
    }

    // 公開方法，供其他腳本檢查遊戲是否仍活躍
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
        // 重設變數
        gameover = false;
        mouseClickCount = 0;
        timer = 60f;
        gameWon = false;
        mouseset = new List<mouseset>();
        // 初次收集場景中的老鼠物件
        GetMouse();
        backbutton.SetActive(false);
    }
    public void BackGame()
    {
        SceneManager.LoadScene("Prologue");
    }
}

