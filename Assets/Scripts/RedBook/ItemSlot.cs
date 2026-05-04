using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlot : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public Button useButton;
    public TextMeshProUGUI Description;
    public TextMeshProUGUI amount;
    public Image Tag;
    public TextMeshProUGUI Tagtext;
    public Itemuse itemuse;
    public RedBook redbook;
    private ItemData data;
    private InventoryUI uiRef;
    public SkillManager skillmanager;

    public void Setup(ItemData item, InventoryUI ui)
    {
        data = item;
        uiRef = ui;

        amount.text = item.amount.ToString();
        icon.sprite = item.icon;
        nameText.text = item.itemName;
        Description.text = item.description;
        Tag.sprite = item.Tag;
        Tagtext.text = item.Tagtext;

        // 先移除舊的監聽器，避免重複綁定
        useButton.onClick.RemoveAllListeners();

        // 找到各個管理器實例
        itemuse = FindObjectOfType<Itemuse>();
        redbook = FindObjectOfType<RedBook>();
        skillmanager = FindObjectOfType<SkillManager>();

        // 綁定新的點擊事件
        useButton.onClick.AddListener(OnUseButtonClicked);

        // === ★ 新增：根據遊戲狀態決定按鈕是否可點擊與反灰 ===
        if (GameStateManager.Instance != null)
        {
            // 如果在戰鬥中，interactable 為 true (可點擊、正常顏色)
            // 如果不在戰鬥中，interactable 為 false (不可點擊、自動變灰)
            useButton.interactable = GameStateManager.Instance.IsState(GameState.Battle);
        }
        else
        {
            // 防呆機制：如果找不到 GameStateManager，預設鎖住
            useButton.interactable = false;
        }
        // =========================================================
    }

    public void OnUseButtonClicked()
    {
        if (data == null) return;

        if (skillmanager == null)
        {
            skillmanager = FindObjectOfType<SkillManager>();
            if (skillmanager == null)
            {
                Debug.LogError("找不到 SkillManager！");
                // ★ 修改：直接使用，不跳確認面板
                uiRef.UseItemDirectly(data);
                return;
            }
        }

        // ★ 修改：正確判斷目前是否在「戰鬥狀態」中
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsState(GameState.Battle))
        {
            TriggerBattleItemUse();

            // 戰鬥中使用完道具（等待選頭像），把背包關起來
            if (redbook != null)
            {
                redbook.CloseRedBook(); // 確保這裡呼叫的是你 Redbook 裡面正確的關閉方法
            }
        }
        else
        {
            // 不在戰鬥中 → 走非戰鬥的直接使用流程
            // ★ 修改：直接使用，不跳確認面板
            uiRef.UseItemDirectly(data);
        }
    }

    public void TriggerBattleItemUse()
    {
        if (skillmanager == null || itemuse == null)
        {
            Debug.LogError("SkillManager 或 Itemuse 為 null！");
            return;
        }

        int currentPlayerIndex = skillmanager.battlesystem.currentPlayerIndex;
        skillmanager.ClearAllHighlights();

        // 根據道具名稱呼叫對應的 Trigger 方法
        switch (data.itemName)
        {
            case "神聖罐罐！": // 請注意你資料庫裡面的名稱是否有驚嘆號，要完全一致喔！
            case "Can":       // 保險起見，如果是代號也支援
                skillmanager.pendingSkill = "Can";
                skillmanager.pendingPlayerIndex = currentPlayerIndex;
                skillmanager.dialogueText.text = $"請點擊一個玩家頭像以使用 {data.itemName}！";
                Debug.Log($"背包道具 {data.itemName} 等待頭像選擇（玩家 {currentPlayerIndex}）");
                itemuse.TriggerCanForPlayer(currentPlayerIndex);
                break;

            case "貓草":
            case "Grass":
                skillmanager.pendingSkill = "Grass";
                skillmanager.pendingPlayerIndex = currentPlayerIndex;
                skillmanager.dialogueText.text = $"請點擊一個玩家頭像以使用 {data.itemName}！";
                Debug.Log($"背包道具 {data.itemName} 等待頭像選擇（玩家 {currentPlayerIndex}）");
                itemuse.TriggerGrassForPlayer(currentPlayerIndex);
                break;

            case "凍乾":
            case "Drying":
                skillmanager.pendingSkill = "Drying";
                skillmanager.pendingPlayerIndex = currentPlayerIndex;
                skillmanager.dialogueText.text = $"請點擊一個玩家頭像以使用 {data.itemName}！";
                Debug.Log($"背包道具 {data.itemName} 等待頭像選擇（玩家 {currentPlayerIndex}）");
                itemuse.TriggerDryingForPlayer(currentPlayerIndex);
                break;

            case "鮪魚肉泥":
            case "Tuna":
                itemuse.TriggerTunaForPlayer(currentPlayerIndex);
                skillmanager.dialogueText.text = $"<b>{skillmanager.playerUnits[currentPlayerIndex].unitName}</b> 選擇了 <b>鮪魚肉泥</b>\n恢復我方存活全體成員 「最大血量」的 30%";
                break;

            default:
                Debug.LogWarning($"背包中使用未知的戰鬥道具：{data.itemName}");
                // ★ 修改：未知的道具也直接使用
                uiRef.UseItemDirectly(data);
                break;
        }
    }
}