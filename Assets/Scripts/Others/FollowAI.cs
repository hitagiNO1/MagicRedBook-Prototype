using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class FollowAI : MonoBehaviour
{
    private NavMeshAgent agent;              // NavMeshAgent 控制移動
    public GameObject ObjectToFollow;        // 要跟隨的目標物件

    [Header("動畫控制 (Spine)")]
    public SkeletonAnimation skeletonAnimation;       // Spine 動畫元件
    public AnimationReferenceAsset idle, walking;     // 動畫資源（待機 / 走路）
    private string currentAnimation;                  // 紀錄目前的動畫名稱，避免重複切換
    public float walkspeed = 3;
    public float runspeed = 8;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // 關閉自動旋轉，因為我們只要左右翻轉
        agent.updateRotation = false;
    }

    void Update()
    {
        if (agent != null && agent.isOnNavMesh && ObjectToFollow != null)
        {
            float distance = Vector3.Distance(transform.position, ObjectToFollow.transform.position);

            // --- 判斷距離，控制移動與動畫 ---
            if (distance < 5f)
            {
                // 停下來，播放 Idle 動畫
                agent.isStopped = true;
                SetAnimation(idle, true, 1f);
            }
            else if (distance >= 5f && distance < 10f)
            {
                // 行走跟隨
                agent.isStopped = false;
                agent.speed = walkspeed;
                agent.SetDestination(ObjectToFollow.transform.position);

                SetAnimation(walking, true, 2f);
            }
            else if (distance >= 11f)
            {
                // （可選擇加跑步邏輯）
                agent.isStopped = false;
                agent.speed = runspeed;
                agent.SetDestination(ObjectToFollow.transform.position);

                // 如果有跑步動畫，可以在這裡加上
                // SetAnimation(running, true, 2.5f);
            }

            // --- 根據目標位置決定朝向（只翻轉 X） ---
            Vector3 dir = ObjectToFollow.transform.position - transform.position;

            if (dir.x > 0.1f)
                transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);   // 面向右
            else if (dir.x < -0.1f)
                transform.localScale = new Vector3(-0.1f, 0.1f, 0.1f);  // 面向左
        }
        else
        {
            Debug.LogWarning("Agent not on NavMesh or missing target!");
        }
    }

    /// <summary>
    /// 設定 Spine 動畫
    /// </summary>
    /// <param name="animation">要播放的動畫資源</param>
    /// <param name="loop">是否循環</param>
    /// <param name="timeScale">動畫播放速度</param>
    public void SetAnimation(AnimationReferenceAsset animation, bool loop, float timeScale)
    {
        // 如果動畫是空的，或已經在播放，就不重複設定
        if (animation == null || animation.name.Equals(currentAnimation))
            return;

        // 設定 Spine 動畫
        skeletonAnimation.state.SetAnimation(0, animation, loop).TimeScale = timeScale;

        // 記錄目前播放中的動畫名稱
        currentAnimation = animation.name;
    }
}
