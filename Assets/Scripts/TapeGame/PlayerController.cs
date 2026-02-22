using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("軌道高度設定")]
    public float groundY = -3.0f; // 地面層高度
    public float airY = 1.5f;     // 空中層高度

    [Header("平滑移動設定")]
    public float moveSpeed = 30f; // 角色上下移動的速度 (數值越大越快)
    private float targetY;        // 角色目前「應該」要去的高度

    [Header("按鍵設定")]
    public KeyCode groundKey1 = KeyCode.F;
    public KeyCode groundKey2 = KeyCode.D;
    public KeyCode airKey1 = KeyCode.J;
    public KeyCode airKey2 = KeyCode.K;

    [Header("自動落地設定")]
    public float airStayTime = 0.5f;    // 在空中停留多久後自動掉下來
    private float currentAirTimer = 0f; // 內部計時器

    [Header("攻擊判定設定")]
    public float attackRange = 1.5f;  // 攻擊圓圈大小
    public float attackOffset = 1.0f; // 攻擊點在角色前方多遠
    public LayerMask enemyLayer;      // 指定只打 Enemy 層

    // 內部狀態
    private bool isGrounded = true;

    void Start()
    {
        // 遊戲開始時，設定目標為地面，並瞬間把角色放過去
        targetY = groundY;
        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
    }

    void Update()
    {
        // --- 1. 偵測玩家輸入 ---

        if (Input.GetKeyDown(groundKey1) || Input.GetKeyDown(groundKey2))
        {
            MoveToLane(true);
            CheckHit(true);
        }

        if (Input.GetKeyDown(airKey1) || Input.GetKeyDown(airKey2))
        {
            MoveToLane(false);
            CheckHit(false);
            currentAirTimer = 0f;
        }

        // --- 2. 空中自動落地邏輯 ---
        if (!isGrounded)
        {
            currentAirTimer += Time.deltaTime;
            if (currentAirTimer >= airStayTime)
            {
                MoveToLane(true);
            }
        }

        // --- 3. 執行平滑移動 ---
        Vector3 targetPos = new Vector3(transform.position.x, targetY, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
    }

    // ==========================================
    // 下面這兩個就是剛才系統說找不到的函數，
    // 它們必須獨立在 Update() 外面！
    // ==========================================

    void MoveToLane(bool goGround)
    {
        isGrounded = goGround;

        if (goGround)
        {
            currentAirTimer = 0f;
        }

        targetY = goGround ? groundY : airY;
    }

    void CheckHit(bool isGroundAttack)
    {
        float hitTargetY = isGroundAttack ? groundY : airY;
        Vector2 attackPos = new Vector2(transform.position.x + attackOffset, hitTargetY);

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPos, attackRange, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            if (Mathf.Abs(enemy.transform.position.y - hitTargetY) > 1.0f)
            {
                continue;
            }

            if (GameManager.instance != null)
            {
                GameManager.instance.AddScore(100);
            }

            Destroy(enemy.gameObject);
            break;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Vector2 groundAttackPos = new Vector2(transform.position.x + attackOffset, groundY);
        Gizmos.DrawWireSphere(groundAttackPos, attackRange);

        Vector2 airAttackPos = new Vector2(transform.position.x + attackOffset, airY);
        Gizmos.DrawWireSphere(airAttackPos, attackRange);
    }
}