using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class usercontrol : MonoBehaviour
{
    private List<HoleState> holeStates; // 改用 HoleState
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
        holeStates = new List<HoleState>();
        mouseborn = FindObjectOfType<mouseborn>();
        if (mouseborn == null)
        {
            Debug.LogError("未找到 mouseborn 組件！");
        }
        GetHoles();
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
            GetHoles();
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
                    if (holeState != null)
                    {
                        Debug.Log("點擊到老鼠：" + hit.collider.gameObject.name);
                        if (mouseborn != null)
                        {
                            mouseborn.OnObjectClicked(holeState.Hole);
                        }
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

    public void GetHoles()
    {
        if (gameover || gameWon)
        {
            return;
        }
        GameObject[] holes = GameObject.FindGameObjectsWithTag("mouse");
        foreach (var o in holes)
        {
            HoleState holeState = o.GetComponent<HoleState>();
            if (holeState != null && !holeStates.Contains(holeState))
            {
                holeStates.Add(holeState);
            }
        }
        holes = GameObject.FindGameObjectsWithTag("C4");
        foreach (var o in holes)
        {
            HoleState holeState = o.GetComponent<HoleState>();
            if (holeState != null && !holeStates.Contains(holeState))
            {
                holeStates.Add(holeState);
            }
        }
    }

    public void CleanUpHoleList()
    {
        holeStates.RemoveAll(hole => hole == null || hole.gameObject == null);
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
        holeStates = new List<HoleState>();
        GetHoles();
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