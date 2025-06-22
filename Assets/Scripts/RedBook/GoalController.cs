using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GoalController : MonoBehaviour
{
    [System.Serializable]
    public class ObjectiveCondition
    {
        public GameScene requiredScene;
        [TextArea(1, 3)] public string objectiveText;
    }

    public GameController gameController;                // 從 Inspector 拖進來
    public TMP_Text objectiveTextUI;                     // 顯示目標提示的 UI
    public List<ObjectiveCondition> objectiveConditions; // 可設定多個條件提示
    [TextArea(1, 3)] public string defaultText = "目前沒有明確的目標。";
    public void UpdateObjective()
    {
        // 從後往前找，代表越後面條件優先
        for (int i = objectiveConditions.Count - 1; i >= 0; i--)
        {
            var condition = objectiveConditions[i];
            if (gameController.ScenePlayedState.TryGetValue(condition.requiredScene, out bool played) && played)
            {
                objectiveTextUI.text = condition.objectiveText;
                return;
            }
        }

        // 如果沒有條件符合，就顯示預設提示
        objectiveTextUI.text = defaultText;
    }
}
