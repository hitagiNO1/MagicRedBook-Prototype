using UnityEngine;

public class BloomFlicker : MonoBehaviour
{
    public Renderer bookRenderer;
    public Transform target;

    [Header("°{Ã{±±¨î")]
    public float maxDistance = 10f;
    public float minSpeed = 1f;
    public float maxSpeed = 8f;
    public float minIntensity = 1f; // ³Ì·t
    public float maxIntensity = 5f; // ³Ì«G

    private Material mat;
    private Color emissionBase;

    void Start()
    {
        mat = bookRenderer.material;
        emissionBase = mat.GetColor("_GlowColor");
    }

    void Update()
    {
        if (target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);
        float t = Mathf.InverseLerp(maxDistance, 0f, distance);
        float speed = Mathf.Lerp(minSpeed, maxSpeed, t);

        float intensity = Mathf.Lerp(minIntensity, maxIntensity,
                                     (Mathf.Sin(Time.time * speed) + 1f) * 0.5f);

        mat.SetColor("_GlowColor", Color.white * intensity);

        //Debug.Log($"Dist={distance:F2}, t={t:F2}, speed={speed:F2}, intensity={intensity:F2}");
    }
}
