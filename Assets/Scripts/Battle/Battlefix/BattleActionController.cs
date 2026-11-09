using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleActionController : MonoBehaviour
{
    public StatsManager playerStats;
    public StatsManager enemyStats;

    // 這個方法可能綁定在你的攻擊動畫事件 (Animation Event) 上
    public void OnAttackHit()
    {
        // 1. 計算傷害
        float damageToTake = CombatCalculator.CalculatePhysicalDamage(playerStats, enemyStats);

        // 2. 顯示傷害數字 UI (假設你有這個系統)
        // DamageUIManager.ShowDamage(enemyStats.transform.position, damageToTake);

        // 3. 呼叫敵人的扣血機制 (假設敵人有 Health 腳本)
        // enemyHealth.TakeDamage(damageToTake);

        Debug.Log($"對敵人造成了 {damageToTake} 點傷害！");
    }
}