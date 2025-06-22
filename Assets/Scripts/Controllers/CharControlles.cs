using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

public class CharControlles : MonoBehaviour
{
    public float vSpeed = 3f; // 移動速度
    public float hSpeed = 3f; 
    public float h;
    public float v;
    float damping = 2.5f;
    public GameController Gamescript;
    //public FollowObject isFollowing;
    //public MeshRenderer meshRenderer;
    private bool previousIsDone; // 保存上一個IsDone的狀態

    public string curruntstate;// 劇情的狀態
    public BattleSystem state;// 戰鬥的狀態
    public SkeletonAnimation skeletonAnimation;
    public AnimationReferenceAsset idle, walking , RedBook;
    public string curruntanimation;
    private void Start()
    {
        
        previousIsDone = Gamescript.IsDone;
        curruntstate = "Idle";
        //SetCharState(curruntstate);
    }

    void FixedUpdate() 
    {
        AnimateState();
        HandleMovement();
    }

    private void HandleMovement()   //腳色移動
    {

        if (GameStateManager.Instance.IsState(GameState.Playing) || GameStateManager.Instance.IsState(GameState.Story))
        {
            h = Input.GetAxis("Horizontal"); // 獲取水平軸向按鍵
            v = Input.GetAxis("Vertical");   // 獲取垂直軸向按鍵
        }
        else 
        {
            h = Mathf.MoveTowards(h, 0f, damping * Time.deltaTime);
            v = Mathf.MoveTowards(v, 0f, damping * Time.deltaTime);
        }
        Vector3 movement = new Vector3(h * hSpeed, 0, v * vSpeed) * Time.deltaTime;
        transform.Translate(movement);       // 根據按鍵來移動
    }
    private void HideChar()  //處理腳色於對話場景時消失
    {
        if (state.state != BattleState.END)
        {
            GetComponentInChildren<MeshRenderer>().enabled = false;
        }
        else
        {
            GetComponentInChildren<MeshRenderer>().enabled = true;
        }
    }

    public void setanimation(AnimationReferenceAsset animation, bool loop, float timeScale)     //設定腳色撥放動畫
    {
        if (animation.name.Equals(curruntanimation))  //讓動畫撥放一次就好不是每幀都撥放
        {
            return;
        }
        skeletonAnimation.state.SetAnimation(0, animation, loop).TimeScale = timeScale;
        curruntanimation = animation.name;
    }

    public void SetCharState(string State)  //設定現在動畫為什麼狀態 用於控制要撥啥動畫
    {
        if (State.Equals("Idle"))
        {
            setanimation(idle, true, 1f);
        }
        else if (State.Equals("walking"))
        {
            setanimation(walking, true, 2f);
        }
        else if (State.Equals("RedBook"))
        {
            setanimation(RedBook, false, 1f);
        }

    }
    public void AnimateState()  //判斷腳色是否行走與他的方向
    {
        if(h !=0 || v != 0)
        {
            SetCharState("walking");
            if (h > 0)
            {
                transform.localScale = new Vector3(-0.1f, 0.1f, 0.1f);
            }
            else
            {
                transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            }
            
        }
        else if(GameStateManager.Instance.IsState(GameState.Redbook))
        {
            SetCharState("RedBook");
        }
        else SetCharState("Idle");

    }
    
}
