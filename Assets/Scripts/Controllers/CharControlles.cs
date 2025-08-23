using System.Collections;
using UnityEngine;
using Spine.Unity;

public class CharControlles : MonoBehaviour
{
    [Header("角色移動設定")]
    public float walkSpeed = 3f;    // 走路速度
    public float runSpeed = 6f;     // 奔跑速度
    private float h;
    private float v;
    private float damping = 2.5f;

    [Header("動畫控制")]
    public SkeletonAnimation skeletonAnimation;
    public AnimationReferenceAsset idle, walking, RedBook;
    private string currentAnimation;

    [Header("劇情設定")]
    public GameScene Scene;
    public GameObject Lyla;

    private void Start()
    {
        SetCharState("Idle");
    }

    void FixedUpdate()
    {
        AnimateState();
        HandleMovement();
        HandleVisibility();
    }
    private void HandleMovement()   // 腳色移動
    {
        if (GameStateManager.Instance.IsState(GameState.Playing) ||
            GameStateManager.Instance.IsState(GameState.Story))
        {
            h = Input.GetAxis("Horizontal");
            v = Input.GetAxis("Vertical");
        }
        else
        {
            h = Mathf.MoveTowards(h, 0f, damping * Time.deltaTime);
            v = Mathf.MoveTowards(v, 0f, damping * Time.deltaTime);
        }

        //Shift 奔跑
        float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        Vector3 movement = new Vector3(h * moveSpeed, 0, v * moveSpeed) * Time.deltaTime;
        transform.Translate(movement, Space.World);
    }
    public void SetAnimation(AnimationReferenceAsset animation, bool loop, float timeScale)
    {
        if (animation == null || animation.name.Equals(currentAnimation))
            return;

        skeletonAnimation.state.SetAnimation(0, animation, loop).TimeScale = timeScale;
        currentAnimation = animation.name;
    }
    public void SetCharState(string state)
    {
        switch (state)
        {
            case "Idle":
                SetAnimation(idle, true, 1f);
                break;
            case "Walking":
                SetAnimation(walking, true, 2f);
                break;
            case "RedBook":
                SetAnimation(RedBook, false, 1f);
                break;
        }
    }
    public void AnimateState()
    {
        if (h != 0 || v != 0)
        {
            SetCharState("Walking");

            //角色朝向
            if (h > 0)
                transform.localScale = new Vector3(-0.1f, 0.1f, 0.1f);
            else if (h < 0)
                transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        }
        else if (GameStateManager.Instance.IsState(GameState.Redbook))
        {
            SetCharState("RedBook");
        }
        else
        {
            SetCharState("Idle");
        }
    }
    private void HandleVisibility() //處理角色在戰鬥狀態下隱藏
    {
        var meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer == null) return;

        if (GameStateManager.Instance.IsState(GameState.Battle))
            meshRenderer.enabled = false;
        else
            meshRenderer.enabled = true;
    }
    public void LylaShow()
    {
        if (GameController.Instance.ScenePlayedState.ContainsKey(Scene))
        {
            Lyla.SetActive(true); // 如果劇情已經播過，則顯示 Lyla
        }
    }
}
