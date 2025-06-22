using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObject : MonoBehaviour
{
    private Transform target;           // 要跟随的目標
    public float followSpeed = 5f;      // 跟随速度，值越大跟随越快
    private Vector3 offset;             // 目標與父物體的偏移量
    public bool isFollowing = false;   
    bool inRange = false;

    void Start()
    {

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && inRange == true)
        {
            offset = transform.parent.position - target.position;   // 計算目標與父物件間的初始偏移量
            isFollowing = !isFollowing; 
        }
        if (isFollowing && target != null)
        {
            // 計算目標位置，只在X軸和Z軸上偏移，Y軸位置不變
            Vector3 targetPosition = new Vector3(target.position.x + offset.x, transform.parent.position.y, target.position.z + offset.z);
            // 使用插值使子父物體跟隨移動
            transform.parent.position = Vector3.Lerp(transform.parent.position, targetPosition, followSpeed * Time.deltaTime);
        }
    }
    void OnTriggerEnter(Collider other)     //檢測角色是否進入功能範圍，並賦予跟隨對象
    {
        if (other.gameObject.tag == "player")
        {
            inRange = true;
            target = other.transform;
        }
    }
    void OnTriggerExit(Collider other)      //檢測角色是否進入功能範圍，取消跟隨對象
    {
        if (other.gameObject.tag == "player")
        {
            inRange = false;
            target = null;
        }
    }
}
