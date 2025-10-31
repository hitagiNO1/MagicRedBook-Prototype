/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum Holenumber
{
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Invalid
}
public class mouseset : MonoBehaviour
{
    private Holenumber hole;
    private Vector3 temPos;
    private Transform parentTrans;



    // Start is called before the first frame update
    void Start()
    {
        SetPosition(hole);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Holenumber GetHole
    {
        set { hole = value; }
        get { return hole; }
    }

    public Transform GetParent
    {
        set { parentTrans = value; }
        get { return parentTrans; }
    }

    private void SetPosition(Holenumber hole)
    {
        switch (hole)
        {
            case Holenumber.One:
                temPos = new Vector3(5f,2f,0f);
                break;
            case Holenumber.Two:
                temPos = new Vector3(0f, 2f, 0f);
                break;
            case Holenumber.Three:
                temPos = new Vector3(-5f, 2f, 0f);
                break;
            case Holenumber.Four:
                temPos = new Vector3(5f, -0.5f, 0f);
                break;
            case Holenumber.Five:
                temPos = new Vector3(0f, -0.5f, 0f);
                break;
            case Holenumber.Six:
                temPos = new Vector3(-5f, -0.5f, 0f);
                break;
            case Holenumber.Seven:
                temPos = new Vector3(5f, -3f, 0f);
                break;
            case Holenumber.Eight:
                temPos = new Vector3(0f, -3f, 0f);
                break;
            case Holenumber.Nine:
                temPos = new Vector3(-5f, -3f, 0f);
                break;
        }
        transform.position = temPos;
        transform.SetParent(parentTrans, false);
    }

}
*/