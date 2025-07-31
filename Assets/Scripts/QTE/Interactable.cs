using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Interactable : MonoBehaviour
{
    public ItemData can;
    public ItemData tuna;
    public ItemData grass;
    public ItemData drying;
    public GameObject promptUI; // 提示UI（例如Canvas上的Text或Image）
    private bool isPlayerInRange = false;
    private Transform playerTransform; // 儲存玩家 Transform
    public locationstore locationstore;

    void Start()
    {
        promptUI.SetActive(false); // 開始時隱藏提示
    }

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            LoadNextScene();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            promptUI.SetActive(true);
            isPlayerInRange = true;
            playerTransform = other.transform; // 儲存玩家位置
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            promptUI.SetActive(false);
            isPlayerInRange = false;
            playerTransform = null;
        }
    }

    void LoadNextScene()
    {
        if (playerTransform != null)
        {
            locationstore.Instance.SavePlayerPosition(playerTransform.position); // 儲存位置
            SceneManager.LoadScene("2D_test");
        }
    }
}
