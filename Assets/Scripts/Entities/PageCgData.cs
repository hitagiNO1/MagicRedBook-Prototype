using UnityEngine;

[CreateAssetMenu(fileName = "New CG", menuName = "CG Gallery/CG Data")]
public class PageCgData : ScriptableObject
{
    public int cgID;
    public string cgName;
    public Sprite cgImage;
    [TextArea]
    public string description;
}

