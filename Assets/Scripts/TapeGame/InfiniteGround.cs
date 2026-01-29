using UnityEngine;

public class InfiniteGround : MonoBehaviour
{
    
    private float moveSpeed;

    private BoxCollider2D groundCollider;
    private float groundWidth;

    void Start()
    {
        groundCollider = GetComponent<BoxCollider2D>();

        // --- 修改這行 ---
        // 我們要乘上 transform.localScale.x，這樣才會抓到「拉長後」的真實寬度
        groundWidth = groundCollider.size.x * transform.localScale.x;
        // ----------------
    }

    void Update()
    {
        // 為了方便測試，這邊先寫死速度，或者去讀取 WorldMover.globalSpeed
        moveSpeed = 10f;

        // 1. 向左移動
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);

        // 2. 檢查是否跑出畫面左側
        // 當 X 座標小於 "-寬度" 時，代表已經完全看不見了 (假設中心點在正中間)
        if (transform.position.x < -groundWidth)
        {
            Reposition();
        }
    }

    void Reposition()
    {
        // 稍微減去 0.1f 讓它們重疊一點點，避免因為浮點數誤差產生的微小裂縫
        Vector2 offset = new Vector2(groundWidth * 2f - 6f, 0);
        transform.position = (Vector2)transform.position + offset;
    }
}