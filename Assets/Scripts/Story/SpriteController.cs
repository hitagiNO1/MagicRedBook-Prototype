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

    private bool isVisible = true; //讓disappear消失一次
    private Coroutine destroyCoroutine; // 記錄目前的銷毀協程

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
        CancelDestroy(); // 確保出場時不會被之前的計時器殺掉
        isVisible = true;
        animator.SetTrigger("Show");
        rect.localPosition = coords;
        Flip();
    }

    public void Disappear()
    {
        if (!isVisible) return;
        animator.SetTrigger("Hide");
        isVisible = false;
        destroyCoroutine = StartCoroutine(WaitAndDisable());
    }

    public void Show()
    {
        CancelDestroy(); // 確保顯示時不會被之前的計時器殺掉
        isVisible = true;
        first.SetActive(true);
        Flip();
    }

    public void Hide()
    {
        first.SetActive(false);
    }

    public void Move(Vector2 coords)
    {
        rect.localPosition = coords;
        first.SetActive(true);
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

    // 專門用來取消銷毀的方法
    private void CancelDestroy()
    {
        if (destroyCoroutine != null)
        {
            StopCoroutine(destroyCoroutine);
            destroyCoroutine = null;
        }
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
        if (switcher.GetImage() != sprite)
        {
            switcher.SwitchImage(sprite);
        }
    }
}