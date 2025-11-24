using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class AnimEvent : MonoBehaviour
{
    public ParticleSystem DustVFX;

    //public Volume postProcessVolume; 
    //private Vignette vignette;
    /*
    [Header("動畫可控制參數")]
    [Range(0f, 1f)]
    public float vignetteValue = 0f;

    void Start()
    {
        // 嘗試抓取 Vignette
        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGet(out vignette);
        }
    }
    void Update()
    {
        // 每幀把 vignetteValue 套用到 Vignette
        if (vignette != null)
        {
            vignette.intensity.Override(vignetteValue);
        }
    }
    */
    public void Play_DustVFX()
    {
        DustVFX.Play();
    }
    public void DeleteObject()
    {
        Destroy(transform.parent.gameObject);
    }
    public void Shake1()
    {
        EffectManager.Instance.ShakeCamera(1f, 3f, 0.3f);
    }
    public void Shake2()
    {
        EffectManager.Instance.ShakeCamera(2f, 5f, 0.3f);
    }
    public void Shake3()
    {
        EffectManager.Instance.ShakeCamera(3f, 0.1f, 0.5f);
    }
    public void Shake4()
    {
        EffectManager.Instance.ShakeCamera(1.5f, 0.1f, 1f);
    }
    public void Red()
    {
        EffectManager.Instance.FlashRed(0.5f);
    }
    public void FlashRedAll()
    {
        EffectManager.Instance.FlashRedAll(1f);
    }

    public void Def()
    {
        EffectManager.Instance.FlashRedAll(1f);
    }
    public void Heal()
    {
        EffectManager.Instance.FlashRedAll(1f);
    }
    public void Def4()
    {
        EffectManager.Instance.Def4();
    }
}
