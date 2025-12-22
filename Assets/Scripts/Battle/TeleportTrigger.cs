using UnityEngine;

public class TeleportTrigger : MonoBehaviour
{
    [Header("傳送座標設定")]
    [Tooltip("請在此輸入你想要傳送到的 X, Y, Z 座標")]
    public Vector3 targetPosition;

    [Tooltip("只有這個 Tag 的物件會觸發")]
    public string targetTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        // 1. 確認撞到的是不是玩家
        if (other.CompareTag(targetTag))
        {
            // --- 安全措施：處理 CharacterController ---
            // 如果角色有 CharacterController，直接改 Position 有時會失效，必須先關閉
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // 2. 執行傳送 (直接指定座標)
            other.transform.position = targetPosition;

            // --- 安全措施：處理 Rigidbody ---
            // 如果有剛體，歸零速度以免傳送後帶著慣性飛出去
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 重新開啟 CharacterController
            if (cc != null) cc.enabled = true;

            Debug.Log($"已傳送角色到座標: {targetPosition}");
        }
    }
}