using UnityEngine;

public class MapTrigger : MonoBehaviour
{
    [Header("設定")]
    public GameObject linkedUIIcon;

    [Tooltip("玩家的 Tag")]
    public string playerTag = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            // 通知管理器：把我的 UI 圖示打開！
            MiniMapController.instance.UpdateLocation(linkedUIIcon);
        }
    }
}