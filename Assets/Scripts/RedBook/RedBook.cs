using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Cinemachine;

public class RedBook : MonoBehaviour
{
    public GameObject menuUI;
    public GameObject[] pages; // 0: Party, 1: Inventory, 2: Map, 3: Monster
    public GameObject PageTag;
    public Sprite[] image;
    public CharStateUI BlackState;
    public CharStateUI LylaState;
    public GoalController Goal;
    private int currentPage = 0;
    private bool isOpen = false;
    private Animator animator;

    //public CinemachineVirtualCamera BookCamera = null;
    private void Start()
    {
        animator = GetComponent<Animator>();
        //BookCamera.enabled = false;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (GameStateManager.Instance.IsState(GameState.Playing))
            {
                // 打開紅書 
                GameStateManager.Instance.SetState(GameState.Redbook);
                StartCoroutine(OpenBook());
            }
            else if (isOpen == true)
            {
                // 關閉紅書
                isOpen = false;
                animator.SetTrigger("DOWN");
                //BookCamera.enabled = false;
                // 等動畫播完後再執行關閉
                StartCoroutine(CloseBook());
                GameStateManager.Instance.SetState(GameState.Playing);
            }
        }
        if (isOpen)
        {
            BlackState.SetHUD();
            LylaState.SetHUD();
        }
    }
    private IEnumerator CloseBook()
    {
        // 等待 Animator 進入 DOWN 狀態
        yield return new WaitForSeconds(0.7f);
        // 動畫播放完畢，才關閉 UI 與切換狀態
        menuUI.SetActive(false);
    }
    private IEnumerator OpenBook()
    {
        //延遲紅書上升動畫攝影機抬頭
        yield return new WaitForSeconds(2f);
        isOpen = true;
        //BookCamera.enabled = true;
        animator.SetTrigger("UP");
        menuUI.SetActive(true);
        ShowPage(0);                // 預設顯示 Party
        Goal.UpdateObjective();
    }
    public void ShowPage(int index)
    {
        currentPage = index;
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
            PageTag.GetComponent<Image>().sprite = image[index];
        }
    }
}
