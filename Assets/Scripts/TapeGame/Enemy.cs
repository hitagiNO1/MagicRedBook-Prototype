using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 10f; // 記得要跟地板速度一樣

    void Update()
    {
        // 往左跑
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        // 檢查是否跑出畫面左邊 (假設 -15 是畫面外)
        if (transform.position.x < -15f)
        {
            // --- 這裡就是「沒打到方塊扣血」的關鍵 ---

            // 呼叫 GameManager 扣 1 滴血
            if (GameManager.instance != null)
            {
                GameManager.instance.TakeDamage(1);
            }

            // 扣完血後銷毀自己，不然會一直扣
            Destroy(gameObject);
        }
    }

    // (選用功能) 如果你希望角色"撞到"方塊也要扣血，可以保留這個
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.TakeDamage(1);
            }
            Destroy(gameObject);
        }
    }
}