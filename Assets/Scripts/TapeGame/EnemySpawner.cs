using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("要生成的敵人模板")]
    public GameObject enemyPrefab;

    [Header("生成設定")]
    public float spawnInterval = 2.0f; // 每幾秒生一隻
    public float spawnX = 12f;         // 在畫面右邊多遠的地方生成 (X座標)

    [Header("軌道高度 (要跟 PlayerController 的數值一樣)")]
    public float groundY = -3.0f;
    public float airY = 1.5f;

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        // 時間到了嗎？
        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0f; // 重置計時器
        }
    }

    void SpawnEnemy()
    {
        // 1. 隨機決定生成在 "上" 還是 "下"
        // Random.value 會回傳 0.0 ~ 1.0 之間的數
        float spawnY = (Random.value > 0.5f) ? airY : groundY;

        // 2. 設定生成位置 (X 固定在右邊, Y 隨機, Z 為 0)
        Vector3 spawnPos = new Vector3(spawnX, spawnY, 0);

        // 3. 生成敵人 (Instantiate)
        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}