using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("初始最大血量")]
    public float maxHealth = 100f;

    // 當前血量與死亡狀態 (外部只能讀取，不能隨意修改)
    public float currentHealth { get; private set; }
    public bool isDead { get; private set; }

    // 【核心機制：事件廣播】
    // 當血量改變時觸發，並傳遞 (當前血量, 最大血量) 給監聽者 (如 UI)
    public event Action<float, float> OnHealthChanged;
    // 當角色死亡時觸發
    public event Action OnDied;

    private void Start()
    {
        // 戰鬥開始時，滿血復活
        currentHealth = maxHealth;
        isDead = false;

        // 初始廣播一次，讓血條 UI 顯示正確
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// 受到傷害
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        // 確保血量不會扣成負數
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // 廣播：我的血量改變囉！
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 補血
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;

        // 確保血量不會超過上限
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // 廣播：我的血量改變囉！
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// 死亡邏輯
    /// </summary>
    private void Die()
    {
        isDead = true;
        OnDied?.Invoke(); // 廣播：我死掉囉！
        Debug.Log($"{gameObject.name} 倒下了！");

        // 這裡可以呼叫動畫控制器播放死亡動畫
        // GetComponent<Animator>().Play("Die");
    }
}