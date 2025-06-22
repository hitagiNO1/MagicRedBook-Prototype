using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteSwitcher : MonoBehaviour
{
    //掛在角色上用來換表情或動作的
    public bool isSwitched = false;
    public Image image1;
    public Image image2;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();        //獲取角色的animator
    }

    public void SwitchImage(Sprite sprite)
    {
        //Debug.Log("換臉");
        image2.sprite = sprite;
        animator.SetTrigger("SwitchFirst");
        image1.sprite = sprite;
        animator.SetTrigger("SwitchSecond");
    }

    public void SetImage(Sprite sprite)
    {
        if (!isSwitched)
        {
            image1.sprite = sprite;
        }
        else
        {
            image2.sprite = sprite;
        }
    }

    public Sprite GetImage()
    {
        if (!isSwitched)
        {
            return image1.sprite;
        }
        else
        {
            return image2.sprite;
        }
    }
}
