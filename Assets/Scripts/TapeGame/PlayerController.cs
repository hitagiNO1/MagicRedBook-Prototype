using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("軌道高度")]
    public float groundY = -3.0f;
    public float airY = 1.5f;

    [Header("按鍵設定")]
    public KeyCode groundKey = KeyCode.F;
    public KeyCode airKey = KeyCode.J;

    // --- 新增：攻擊設定 ---
    [Header("攻擊設定")]
    public float attackRange = 1.5f;   // 攻擊範圍 (圓圈半徑)
    public float attackOffset = 1.0f;  // 攻擊點要在主角前方多遠
    public LayerMask enemyLayer;       // 指定要偵測哪一層 (選 Enemy)
    // ---------------------

    void Start()
    {
        MoveToLane(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(groundKey))
        {
            MoveToLane(true);
            CheckHit(true); // --- 新增：移動同時進行攻擊判定 ---
        }

        if (Input.GetKeyDown(airKey))
        {
            MoveToLane(false);
            CheckHit(false); // --- 新增：移動同時進行攻擊判定 ---
        }
    }

    void MoveToLane(bool goGround)
    {
        float targetY = goGround ? groundY : airY;
        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
    }

    // --- 新增：攻擊判定邏輯 ---
    void CheckHit(bool isGround)
    {
 
        float targetY = isGround ? groundY : airY;
        Vector2 attackPos = new Vector2(transform.position.x + attackOffset, targetY);


        Collider2D hitEnemy = Physics2D.OverlapCircle(attackPos, attackRange, enemyLayer);

        // 3. 如果偵測到了東西
        if (hitEnemy != null)
        {
            Debug.Log("打中了！"); // 在 Console 印出訊息方便測試
            Destroy(hitEnemy.gameObject); // 消滅敵人
        }
    }

    void OnDrawGizmosSelected()
    {
        // 畫出地面攻擊範圍預覽
        Vector2 groundAttackPos = new Vector2(transform.position.x + attackOffset, groundY);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundAttackPos, attackRange);

        // 畫出空中攻擊範圍預覽
        Vector2 airAttackPos = new Vector2(transform.position.x + attackOffset, airY);
        Gizmos.DrawWireSphere(airAttackPos, attackRange);
    }
}