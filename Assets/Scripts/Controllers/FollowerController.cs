using UnityEngine;
using Spine.Unity;

public class FollowerController : MonoBehaviour
{
    public Transform player; // 要追蹤的 Transform
    public float stopDistance = 1f;
    public Vector3 offset = new Vector3(-1, 0, 0); // 角色相對於玩家的偏移

    public float minSmoothTime = 0.8f; // 距離遠時移動的最小平滑時間（越小越俐落）
    public float maxSmoothTime = 0.1f;  // 距離近時的最大平滑時間（越大越柔和）

    public AnimationReferenceAsset idle, walk;
    private string currentAnimation;
    private string currentState;
    public SkeletonAnimation skeletonAnimation;

    private Vector3 velocity = Vector3.zero; // SmoothDamp 使用的速度參考值

    private void Start()
    {
        SetCharState("Idle"); // 初始為待機狀態
    }

    private void Update()
    {
        Vector3 targetPos = player.position + offset; // 計算目標位置
        float distance = Vector3.Distance(transform.position, targetPos); // 與玩家的距離

        if (distance > stopDistance)
        {
            // 根據距離計算平滑時間（距離越遠，時間越短 = 越俐落）
            float t = Mathf.Clamp01((distance - stopDistance) / stopDistance);
            float smoothTime = Mathf.Lerp(minSmoothTime, maxSmoothTime, t);

            // 使用 SmoothDamp 平滑移動
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);

            // 自動轉向
            float direction = player.position.x > transform.position.x ? 1f : -1f;
            transform.localScale = new Vector3(0.1f * direction, 0.1f, 0.1f);

            SetCharState("Walking");
        }
        else
        {
            SetCharState("Idle");
        }
    }

    // 設定動畫（防止重複播放）
    public void SetAnimation(AnimationReferenceAsset animation, bool loop, float timeScale)
    {
        if (animation == null || animation.name == currentAnimation) return;
        skeletonAnimation.state.SetAnimation(0, animation, loop).TimeScale = timeScale;
        currentAnimation = animation.name;
    }

    // 切換角色狀態與對應動畫
    public void SetCharState(string state)
    {
        if (state == currentState) return;

        if (state == "Idle") SetAnimation(idle, true, 1f);
        else if (state == "Walking") SetAnimation(walk, true, 1f);

        currentState = state;
    }
}
