using UnityEngine;

public class TextureSwitcher : MonoBehaviour
{
    public Texture2D textureA;    // 第一張圖
    public Texture2D textureB;    // 第二張圖
    public float switchInterval = 0.5f; // 切換間隔 (秒)

    private SpriteRenderer sr;
    private MaterialPropertyBlock mpb;
    private float timer;
    private bool showingA = true;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
        sr.GetPropertyBlock(mpb);

        // 初始貼圖
        mpb.SetTexture("_Sprite", textureA);
        sr.SetPropertyBlock(mpb);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= switchInterval)
        {
            timer -= switchInterval;
            showingA = !showingA;

            sr.GetPropertyBlock(mpb);
            mpb.SetTexture("_Sprite", showingA ? textureA : textureB);
            sr.SetPropertyBlock(mpb);
        }
    }
}
