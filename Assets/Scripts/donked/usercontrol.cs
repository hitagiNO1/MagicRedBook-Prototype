using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class usercontrol : MonoBehaviour
{
    private List<mouseset> mouseset;
    public bool gameover = false;
    public float timer = 60f;
    private int mouseClickCount = 0;
    private const int requiredClicks = 15;
    private bool gameWon = false;
    public TextMeshProUGUI resault;
    public boxgameflow gameflow;
    public GameObject backbutton;
    private mouseborn mouseborn; // 新增：引用 mouseborn 腳本

    void Start()
    {
        mouseset = new List<mouseset>();
        mouseborn = FindObjectOfType<mouseborn>(); // 查找 mouseborn 組件
        if (mouseborn == null)
        {
            Debug.LogError("未找到 mouseborn 組件！");
        }
        GetMouse();
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
                    mouseset mouse = hit.collider.GetComponent<mouseset>();
                    if (mouse != null && mouseborn != null)
                    {
                        mouseborn.OnObjectClicked(mouse.GetHole); // 通知切換圖片為 A
                    }
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
                        Debug.Log("點擊到老鼠：" + hit.collider.gameObject.name);
                        if (mouseborn != null)
                        {
                            mouseborn.OnObjectClicked(mouse.GetHole); // 通知切換圖片為 A
                        }
                        mouseset.Remove(mouse);
                        Destroy(hit.collider.gameObject);
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
        if (gameover || gameWon)
        {
            return;
        }

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

    public void CleanUpMouseList()
    {
        mouseset.RemoveAll(mouse => mouse == null || mouse.gameObject == null);
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
        mouseset = new List<mouseset>();
        GetMouse();
        backbutton.SetActive(false);
        if (mouseborn != null)
        {
            mouseborn.Restart(); // 通知 mouseborn 重啟
        }
    }

    public void BackGame()
    {
        SceneManager.LoadScene("Prologue");
    }
}