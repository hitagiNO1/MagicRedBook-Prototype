using UnityEngine;

public class FloatingUpDownZ : MonoBehaviour
{
    [Header("漂浮設定")]
    public float amplitude = 0.1f;  // 上下移動幅度
    public float speed = 1f;        // 移動速度

    private Vector3 startPos;

    void Start()
    {
        // 記錄起始位置
        startPos = transform.localPosition;
    }

    void Update()
    {
        // 使用 sin 波上下移動
        float offsetY = Mathf.Sin(Time.time * speed) * amplitude;
        transform.localPosition = startPos + new Vector3(0, 0, offsetY);
    }
}
