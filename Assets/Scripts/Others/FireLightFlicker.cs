using UnityEngine;
using UnityEngine.Rendering.Universal; // 確保有引入 URP 2D Light 的命名空間

public class FireLightFlicker : MonoBehaviour
{
    [Header("Light Settings")]
    public Light2D fireLight;

    [Header("Flicker Parameters")]
    public float minIntensity = 0.5f;   // 光線最暗的強度
    public float maxIntensity = 1.2f;   // 光線最亮的強度
    public float flickerSpeed = 3.0f;   // 閃爍的速度

    // 也可以讓光照範圍跟著閃爍，增加真實感
    public float minRadius = 1.0f;
    public float maxRadius = 1.2f;

    private float randomOffset;

    void Start()
    {
        if (fireLight == null)
            fireLight = GetComponent<Light2D>();

        // 加上隨機偏移值，這樣如果畫面中有多個火焰，它們就不會同步閃爍
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (fireLight != null)
        {
            // 產生 Perlin Noise 數值 (介於 0 到 1 之間平滑變動)
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + randomOffset, 0f);

            // 根據 Noise 數值，在最大與最小值之間進行插值 (Lerp)
            fireLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
            fireLight.pointLightOuterRadius = Mathf.Lerp(minRadius, maxRadius, noise);
        }
    }
}