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

    void LateUpdate()
    {
        if (target == null) return;

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
}
