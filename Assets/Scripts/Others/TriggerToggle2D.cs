using UnityEngine;

public class TriggerToggle2D : MonoBehaviour
{
    [Header("要開啟/關閉的目標物體")]
    public GameObject targetObject;

    [Header("觸發者的標籤 (通常是 Player)")]
    public string triggerTag = "Player";

    // 當有物體進入 2D 觸發範圍時執行
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 檢查進入的物體是否帶有我們指定的標籤
        if (other.CompareTag(triggerTag))
        {
            if (targetObject != null)
            {
                targetObject.SetActive(true); // 開啟目標物體
            }
            else
            {
                Debug.LogWarning("請在 Inspector 中指派 Target Object！");
            }
        }
    }

    // 當有物體離開 2D 觸發範圍時執行
    private void OnTriggerExit2D(Collider2D other)
    {
        // 檢查離開的物體是否帶有我們指定的標籤
        if (other.CompareTag(triggerTag))
        {
            if (targetObject != null)
            {
                targetObject.SetActive(false); // 關閉目標物體
            }
        }
    }
}