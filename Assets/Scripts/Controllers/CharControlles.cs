using Spine.Unity;
using System.Collections;
using UnityEngine;

public class CharControlles : MonoBehaviour
{
    [Header("角色移動設定")]
    public float baseWalkSpeed = 4f;    // 基礎走路速度
    public float baseRunSpeed = 10f;   // 基礎奔跑速度

    // 透過屬性計算目前實際速度 = 基礎速度 + 裝備加成
    private float currentWalkSpeed => baseWalkSpeed + speedBonus;
    private float currentRunSpeed => baseRunSpeed + speedBonus;

    [SerializeField]
    private float speedBonus = 0f;     // 鞋子或其他裝備提供的速度增量

    private float h;
    private float v;
    private float damping = 2.5f;

    public SpriteRenderer redbook;

    [Header("動畫控制")]
    public SkeletonAnimation skeletonAnimation;
    public AnimationReferenceAsset idle, walking, RedBook;
    private string currentAnimation;

    private void Start()
    {
        SetCharState("Idle");

        // 初始檢查一次是否有裝備鞋子 (確保遊戲讀取後速度正確)
        ApplySpeedEquipmentEffect();
    }

    void FixedUpdate()
    {
        AnimateState();
        HandleMovement();
        HandleVisibility();
    }

    /// <summary>
    /// 提供給外部呼叫的方法：設定速度加成 (例如劍客鞋子傳入 2f)
    /// </summary>
    public void SetSpeedBonus(float bonus)
    {
        speedBonus = bonus;
        Debug.Log($"角色速度加成已設定為: {bonus}，當前走路速度為: {currentWalkSpeed}");
    }

    /// <summary>
    /// 檢查裝備並套用速度加成的邏輯
    /// </summary>
    public void ApplySpeedEquipmentEffect()
    {
        // 這裡與你的 EquipmentManager 連接
        // 假設 EquipmentManager 有一個方法可以檢查是否穿戴了劍客鞋子
        if (EquipmentManager.Instance != null && EquipmentManager.Instance.HasEquipment("劍客鞋子"))
        {
            SetSpeedBonus(2f);
        }
        else
        {
            SetSpeedBonus(0f);
        }
    }

    private void HandleMovement()   // 角色移動
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

        // 設定移動速度
        float moveSpeed = currentWalkSpeed; // 使用計算後的走路速度

        if (GameStateManager.Instance.IsState(GameState.Playing))
        {
            // Shift 奔跑
            if (Input.GetKey(KeyCode.LeftShift))
                moveSpeed = currentRunSpeed; // 使用計算後的奔跑速度
        }

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
                // 走路動畫速度可以稍微根據移動速度調整，這裡維持原本的 2f
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

            // 角色朝向
            if (h > 0)
                transform.GetChild(0).localScale = new Vector3(1.2f, 1.2f, 1.2f);
            else if (h < 0)
                transform.GetChild(0).localScale = new Vector3(-1.2f, 1.2f, 1.2f);
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

    private void HandleVisibility() // 處理角色在戰鬥狀態下隱藏
    {
        var meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer == null) return;

        if (GameStateManager.Instance.IsState(GameState.Battle))
        {
            meshRenderer.enabled = false;
            redbook.enabled = false;
        }
        else
        {
            meshRenderer.enabled = true;
            redbook.enabled = true;
        }
    }
}