using UnityEngine;

public class WorldMover : MonoBehaviour
{
    // 這裡我們定義一個靜態變數，讓全域速度統一
    // 之後想做「加速」或「暫停」只要改這個變數，所有東西都會同步變快慢
    public static float globalSpeed = 10f;

    void Update()
    {
        // 向左移動 (Vector3.left)
        transform.Translate(Vector3.right * globalSpeed * Time.deltaTime);

        // 優化：如果物體跑出畫面左邊太遠（例如 X < -15），就自我銷毀或是回收
        if (transform.position.x < -15f)
        {
            // 暫時先用 Destroy，之後我們再來談物件池(Object Pooling)
            Destroy(gameObject);
        }
    }
}