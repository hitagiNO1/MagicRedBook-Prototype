using Cinemachine;
using UnityEngine;
public class AnimEvent : MonoBehaviour
{
    public void DeleteObject()
    {
        Destroy(this.gameObject);
    }
    public void Shake1()
    {
        EffectManager.Instance.ShakeCamera(1f, 3f, 0.3f);
    }
    public void Shake2()
    {
        EffectManager.Instance.ShakeCamera(2f, 5f, 0.3f);
    }
    public void Red()
    {
        EffectManager.Instance.FlashRed(0.5f);
    }
}
