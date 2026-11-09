using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 這只是示意代碼，不用馬上寫
public class HealthBarUI : MonoBehaviour
{
    public Health targetHealth; // 綁定目標角色的 Health
    public Image hpBarFill;     // UI 的血條圖片

    private void OnEnable()
    {
        // 訂閱事件：當目標血量改變時，自動執行 UpdateHealthBar
        targetHealth.OnHealthChanged += UpdateHealthBar;
    }

    private void OnDisable()
    {
        // 取消訂閱 (防呆，避免記憶體流失)
        targetHealth.OnHealthChanged -= UpdateHealthBar;
    }

    private void UpdateHealthBar(float current, float max)
    {
        // 更新血條比例
        hpBarFill.fillAmount = current / max;
    }
}
