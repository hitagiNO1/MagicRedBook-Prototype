using UnityEngine;

public class PlayerRunner : MonoBehaviour
{
    // 設定奔跑速度，可以在 Inspector 調整
    public float runSpeed = 10f;

    void Update()
    {
        // 每幀都向右移動 (1, 0, 0) * 速度 * 時間
        transform.Translate(Vector3.right * runSpeed * Time.deltaTime);
    }
}