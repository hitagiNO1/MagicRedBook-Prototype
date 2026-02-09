using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("軌道高度")]
    public float groundY = -3.0f;
    public float airY = 1.5f;

    [Header("按鍵設定")]
    public KeyCode groundKey1 = KeyCode.F;
    public KeyCode groundKey2 = KeyCode.D; // 新增 D 鍵
    public KeyCode airKey1 = KeyCode.J;
    public KeyCode airKey2 = KeyCode.K;    // 新增 K 鍵

    [Header("自動落地設定")]
    public float airStayTime = 0.5f; // 空中滯留時間 (秒)
    private float currentAirTimer = 0f; // 內部計時器

    [Header("攻擊設定")]
    public float attackRange = 1.5f;
    public float attackOffset = 1.0f;
    public LayerMask enemyLayer;

    // 內部狀態
    private bool isGrounded = true;

    void Start()
    {
        MoveToLane(true); // 一開始先在地板
    }

    void Update()
    {
        // --- 1. 地面攻擊 / 主動下潛 (F 或 D) ---
        if (Input.GetKeyDown(groundKey1) || Input.GetKeyDown(groundKey2))
        {
            MoveToLane(true); // 切換到地面
            CheckHit(true);   // 進行地面攻擊判定
        }

        // --- 2. 空中攻擊 / 跳躍 (J 或 K) ---
        if (Input.GetKeyDown(airKey1) || Input.GetKeyDown(airKey2))
        {
            MoveToLane(false); // 切換到空中
            CheckHit(false);   // 進行空中攻擊判定

            // 關鍵：每次按下空中鍵，計時器重置 (讓你可以連續按 J 滯空，如果你想的話)
            currentAirTimer = 0f;
        }

        // --- 3. 自動落地邏輯 ---
        // 如果現在 "不在地面" (isGrounded == false)
        if (!isGrounded)
        {
            // 開始計時
            currentAirTimer += Time.deltaTime;

            // 如果計時超過設定的時間 (0.5秒)
            if (currentAirTimer >= airStayTime)
            {
                MoveToLane(true); // 時間到，強制回到地面
            }
        }
    }

    void MoveToLane(bool goGround)
    {
        isGrounded = goGround;

        // 如果是回到地面，重置計時器 (雖然不重置也沒差，但習慣上歸零比較乾淨)
        if (goGround)
        {
            currentAirTimer = 0f;
        }

        float targetY = goGround ? groundY : airY;

        // 保持 X 和 Z 不變，只改變 Y
        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
    }

    // 攻擊判定邏輯 (跟之前一樣)
    void CheckHit(bool isGroundLayer)
    {
        float targetY = isGroundLayer ? groundY : airY;
        Vector2 attackPos = new Vector2(transform.position.x + attackOffset, targetY);

        Collider2D hitEnemy = Physics2D.OverlapCircle(attackPos, attackRange, enemyLayer);

        if (hitEnemy != null)
        {
            // 呼叫 GameManager 加分
            if (GameManager.instance != null)
            {
                GameManager.instance.AddScore(100);
            }
            Destroy(hitEnemy.gameObject);
        }
    }

    // 畫出輔助線 (跟之前一樣)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 groundAttackPos = new Vector2(transform.position.x + attackOffset, groundY);
        Gizmos.DrawWireSphere(groundAttackPos, attackRange);

        Vector2 airAttackPos = new Vector2(transform.position.x + attackOffset, airY);
        Gizmos.DrawWireSphere(airAttackPos, attackRange);
    }
}