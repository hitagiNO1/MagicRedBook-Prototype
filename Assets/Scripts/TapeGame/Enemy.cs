using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 10f; // 請確保這個速度跟你的地板速度(Ground)一樣，才不會看起來滑步

    void Update()
    {
        // 1. 持續向左移動
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        // 2. 檢查是否跑出畫面左邊 (假設 X < -15 就看不到了)
        if (transform.position.x < -15f)
        {
            Destroy(gameObject); // 銷毀自己，釋放記憶體
        }
    }
}