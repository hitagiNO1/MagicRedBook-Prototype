using UnityEngine;

public class TextureSwitcher : MonoBehaviour
{
    public Sprite imgA;    // 第一張圖
    public Sprite imgB;    // 第二張圖
    public float switchInterval = 0.5f; // 切換間隔 (秒)

    private SpriteRenderer sr;
    private float timer;
    private bool showingA = true;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = imgA;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= switchInterval)
        {
            timer -= switchInterval;
            showingA = !showingA;
            Sprite nextSprite = showingA ? imgA : imgB;

            if (nextSprite == null)
            {
                //Debug.LogError($"{name}: Sprite 為 NULL，可能沒指定！");
            }
            sr.sprite = nextSprite;
        }
    }
}
