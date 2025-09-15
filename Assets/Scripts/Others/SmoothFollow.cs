using UnityEngine;

public class SmoothFollow : MonoBehaviour
{
    [Header("要跟隨的目標")]
    public Transform target;

    [Header("跟隨速度 (數值越小越慢)")]
    public float smoothTime = 0.3f;

    [Header("座標偏移")]
    public Vector3 offset;

    private Vector3 velocity = Vector3.zero;

    [Header("是否啟用跟隨")]
    public bool isFollowing = true; // 預設開啟
    void LateUpdate()
    {
        if (!isFollowing || target == null) return;

        // 目標位置（加上偏移）
        Vector3 targetPosition = target.position + offset;

        // 平滑移動
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );
    }
    /// <summary>
    /// 外部程式可呼叫這個方法來開關跟隨
    /// </summary>
    public void SetFollow(bool follow)
    {
        isFollowing = follow;
    }
}
