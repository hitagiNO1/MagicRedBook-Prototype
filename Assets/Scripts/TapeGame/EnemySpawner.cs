using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("要生成的方塊模板")]
    public GameObject enemyPrefab;

    [Header("生成與發射設定")]
    public float spawnInterval = 1.5f; // 兩次攻擊之間的間隔 (現在變成 BOSS 的游走時間)
    public float telegraphTime = 0.5f; // 預警時間 (BOSS 鎖定軌道後，等多久才發射)
    public Transform shootPoint;

    [Header("軌道高度")]
    public float groundY = -3.0f;
    public float airY = 1.5f;

    [Header("平滑移動設定")]
    public float moveSpeed = 15f;      // BOSS 上下移動的速度
    private float targetY;

    void Start()
    {
        targetY = groundY;

        // 遊戲開始時，啟動「發射循環」協程
        StartCoroutine(SpawnCycle());
    }

    void Update()
    {
        // 讓 BOSS 平滑朝著 targetY 移動
        Vector3 targetPos = new Vector3(transform.position.x, targetY, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
    }

    IEnumerator SpawnCycle()
    {
        // 只要遊戲沒結束，這個迴圈就會一直跑
        while (GameManager.instance != null && !GameManager.instance.isGameOver)
        {
            // ==========================================
            // 階段 1：上下軌道來回切換 (假動作)
            // ==========================================
            float wanderTimer = 0f;
            float switchInterval = 0.25f; // 切換頻率 (每 0.25 秒上下切換一次)

            // 在 spawnInterval 這段時間內，不斷上下橫跳
            while (wanderTimer < spawnInterval)
            {
                // 【核心修改】來回切換：如果目標是地面，就改去空中；反之亦然
                targetY = (targetY == groundY) ? airY : groundY;

                yield return new WaitForSeconds(switchInterval);
                wanderTimer += switchInterval;
            }

            // ==========================================
            // 階段 2：鎖定目標與預警 (Telegraph)
            // ==========================================
            // 隨機決定真正的攻擊軌道 (只能是上層或下層)
            targetY = (Random.value > 0.5f) ? airY : groundY;

            // 等待 BOSS 移動到最後決定的定點，這 0.5 秒就是給玩家的「預警反應時間」
            yield return new WaitForSeconds(telegraphTime);

            // ==========================================
            // 階段 3：發射方塊
            // ==========================================
            float spawnX = (shootPoint != null) ? shootPoint.position.x : transform.position.x;
            Vector3 spawnPos = new Vector3(spawnX, targetY, 0);
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            // 發射後稍微停頓一下 (0.2秒)，表現出「攻擊後搖」，再進入下一輪來回切換
            yield return new WaitForSeconds(0.2f);
        }
    }
}