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
    public Itemuse itemuse; // 新增引用
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

        // 找到 Itemuse 實例（假設場景中只有一個）
        itemuse = FindObjectOfType<Itemuse>();
        redbook = FindObjectOfType<RedBook>();
        skillmanager = FindObjectOfType<SkillManager>();


        // 綁定新的點擊事件
        useButton.onClick.AddListener(OnUseButtonClicked);
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
                uiRef.PromptUse(data);
                return;
            }
        }
        // 判斷目前是否在戰鬥中（使用你既有的 GameStateManager）
        if (GameStateManager.Instance != null)
        {
            TriggerBattleItemUse();
            redbook.CloseRedBook();
            
        }
        else
        {
            // 不在戰鬥中 → 走原本的簡單使用流程（或直接關閉背包）
            uiRef.PromptUse(data); // 保留原本的確認面板流程
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
        // 假設你的四個道具在 ItemData 中的 itemName 分別是 "Can", "Tuna", "Grass", "Drying"
        switch (data.itemName)
        {
            case "神聖罐罐！":
                skillmanager.pendingSkill = data.itemName;
                skillmanager.pendingPlayerIndex = currentPlayerIndex;
                skillmanager.dialogueText.text = $"請點擊一個玩家頭像以使用 {data.itemName}！";
                Debug.Log($"背包道具 {data.itemName} 等待頭像選擇（玩家 {currentPlayerIndex}）");
                itemuse.TriggerCanForPlayer(currentPlayerIndex);
                    break;
            case "貓草":
                skillmanager.pendingSkill = data.itemName;
                skillmanager.pendingPlayerIndex = currentPlayerIndex;
                skillmanager.dialogueText.text = $"請點擊一個玩家頭像以使用 {data.itemName}！";
                Debug.Log($"背包道具 {data.itemName} 等待頭像選擇（玩家 {currentPlayerIndex}）");
                itemuse.TriggerGrassForPlayer(currentPlayerIndex);
                break;
            case "凍乾":
                skillmanager.pendingSkill = data.itemName;
                skillmanager.pendingPlayerIndex = currentPlayerIndex;
                skillmanager.dialogueText.text = $"請點擊一個玩家頭像以使用 {data.itemName}！";
                Debug.Log($"背包道具 {data.itemName} 等待頭像選擇（玩家 {currentPlayerIndex}）");
                itemuse.TriggerDryingForPlayer(currentPlayerIndex);
                break;
            case "鮪魚肉泥":
                itemuse.TriggerTunaForPlayer(currentPlayerIndex);
                skillmanager.dialogueText.text = $"<b>{skillmanager.playerUnits[currentPlayerIndex].unitName}</b> 選擇了 <b>鮪魚肉泥</b>\n恢復我方存活全體成員 「最大血量」的 30%";
                break;


            default:
                Debug.LogWarning($"背包中使用未知的戰鬥道具：{data.itemName}");
                // 可選擇走一般使用流程
                uiRef.PromptUse(data);
                break;
        }

        // 使用後立即關閉紅書（背包），讓玩家回到戰鬥畫面選擇目標
        // 假設你的 RedBook 有關閉方法
        if (itemuse.redbook != null)
        {
            itemuse.redbook.CloseBook(); // 你需要自己加這個方法，或用其他方式關閉
        }
    }
}
