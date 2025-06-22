using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteController : MonoBehaviour
{
    private SpriteSwitcher switcher;
    private Animator animator;
    private RectTransform rect;
    GameObject first;

    private bool isVisible = true;
    private void Awake()
    {
        switcher = GetComponent<SpriteSwitcher>();
        animator = GetComponent<Animator>();
        rect = GetComponent<RectTransform>();
        first = transform.GetChild(0).gameObject;
    }

    public void Setup(Sprite sprite)
    {
        switcher.SetImage(sprite);
    }

    public void Appear(Vector2 coords)
    {
        animator.SetTrigger("Show");
        rect.localPosition = coords;
        Flip();
    }
    public void Disappear()
    {     
        if (!isVisible) return;
        animator.SetTrigger("Hide");
        isVisible = false;
        StartCoroutine(WaitAndDisable());
    }
    public void Show(Vector2 coords)
    {
        rect.localPosition = coords;
        isVisible = true;
        //first.SetActive(true);
        Flip();
    }
    public void Hide()
    {
        first.SetActive(false);
    }
    public void Move(Vector2 coords)
    {
        rect.localPosition = coords;
        Flip();
    }
    private void Flip()
    {
        Vector3 scale = transform.localScale;

        // 如果 x 座標為負，scale.x = 1；反之為 -1
        scale.x = rect.localPosition.x < 0 ? 0.65f : -0.65f;

        transform.localScale = scale;
    }

    private IEnumerator WaitAndDisable()
    {
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }
    /*
public void shock()
{
    animator.SetTrigger("shock");
}
public void shake()
{
    animator.SetTrigger("shake");
}

public void shakeback()
{
    animator.SetTrigger("shakeback");
}
*/
    public void SwitchSprite(Sprite sprite)
    {
        if(switcher.GetImage() != sprite)
        {
            switcher.SwitchImage(sprite);
        }
    }
}
