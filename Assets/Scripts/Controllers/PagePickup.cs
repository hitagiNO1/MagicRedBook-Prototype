using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class PagePickup : MonoBehaviour
{
    public GameObject Page;
    public int PageNum;
    public PlayableAsset timeline;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PageCGUI.Instance.AddPage(Page, PageNum);
            //TimelineManager.Instance.PlayTimeline(timeline);
        }
    }
}
