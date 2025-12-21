using UnityEngine;
using Spine.Unity;

public class SpineColorControl : MonoBehaviour
{
    [Header("角色亮度 (0=黑, 1=原色)")]
    [Range(0, 1)]
    public float darkness = 0.6f; // 預設 0.6，你可以自己拉

    void Start()
    {
        ApplyColor();
    }

    // 為了讓你在編輯模式下也能看到效果 (不用按 Play)
    void OnValidate()
    {
        ApplyColor();
    }

    void ApplyColor()
    {
        var skeletonAnim = GetComponent<SkeletonAnimation>();
        if (skeletonAnim != null && skeletonAnim.skeleton != null)
        {
            // 設定顏色：RGB 變暗，Alpha 保持 1 (不透明)
            skeletonAnim.skeleton.SetColor(new Color(darkness, darkness, darkness, 1f));
        }
    }
}