using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class AfterBattleTL : MonoBehaviour
{
    [Header("怪物死亡")]
    public GameObject[] targetObjects;

    [Header("要播放的劇情")]
    public PlayableAsset TLToPlay;

    private bool hasTriggered = false;

    void Update()
    {
        if (hasTriggered) return;

        if (targetObjects == null || targetObjects.Length == 0) return;

        // 檢查所有目標是否都被刪除
        bool allDestroyed = true;
        foreach (var obj in targetObjects)
        {
            if (obj != null)
            {
                allDestroyed = false;
                break;
            }
        }

        if (allDestroyed)
        {
            // 確保遊戲狀態是 Playing
            if (GameStateManager.Instance.IsState(GameState.Playing))
            {
                TimelineManager.Instance.PlayTimeline(TLToPlay);
                hasTriggered = true;
            }
        }
    }
}
