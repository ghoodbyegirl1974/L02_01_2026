using UnityEngine;
using UnityEngine.InputSystem;

public class BallLauncher_comp_test : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Rigidbody targetRigidbody;
    [SerializeField] private Vector3 initialPosition = new Vector3(0f, 15f, 0f);

    [Header("Base Direction Setting")]
    [Tooltip("発射の基準となる向き（Y軸周りの角度：0度＝Z軸正方向、90度＝X軸正方向）")]
    public float MoveDirection = 0f;

    [Header("Launch Base Parameters")]
    [Tooltip("ショットの最大パワー")]
    [SerializeField] private float maxLaunchPower = 150f;

    [Tooltip("基本の打ち出し角度（ピッチ角）")]
    [Range(0f, 90f)] [SerializeField] private float basePitchAngle = 30f;

    [Header("Force Option")]
    [SerializeField] private ForceMode forceMode = ForceMode.Impulse;

    [Header("Visualization")]
    [Tooltip("射出方向を示す可視化用GameObject（矢印など）のTransform")]
    [SerializeField] private Transform indicatorTransform;

    [Header("Input System Actions")]
    [Tooltip("左右キー入力 (1 / -1)")]
    [SerializeField] private InputAction hArrows;

    [Tooltip("上下キー入力 (1 / -1)")]
    [SerializeField] private InputAction vArrows;

    [Tooltip("角度変更の回転速度 (度/秒)")]
    [SerializeField] private float rotateSpeed = 45f;

    // --- 【外部参照用プロパティ】 ---
    public Rigidbody TargetRigidbody => targetRigidbody;
    public Vector3 InitialPosition => initialPosition;
    public float MaxLaunchPower => maxLaunchPower;
    public float BasePitchAngle => basePitchAngle;

    private void OnEnable()
    {
        hArrows.Enable();
        vArrows.Enable();
    }

    private void OnDisable()
    {
        hArrows.Disable();
        vArrows.Disable();
    }

    private void Start()
    {
        ResetBall();
    }

    private void Update()
    {
        // 1. Input System からの入力受け取り
        float hInput = hArrows.ReadValue<float>();
        float vInput = vArrows.ReadValue<float>();

        // 左右キーで MoveDirection を変更
        MoveDirection += hInput * rotateSpeed * Time.deltaTime;

        // 上下キーで basePitchAngle を変更 (0~90度に制限)
        basePitchAngle += vInput * rotateSpeed * Time.deltaTime;
        basePitchAngle = Mathf.Clamp(basePitchAngle, 0f, 90f);

        // 2. 射出方向可視化用GameObjectの回転更新
        UpdateIndicatorRotation();

        // 3. スペースキーでボールを発射
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            LaunchBall();
        }
    }

    private void OnValidate()
    {
        // Inspectorで値が変更された際にも可視化用GameObjectの回転を更新
        UpdateIndicatorRotation();
    }

    /// <summary>
    /// 可視化用GameObjectの回転を設定した角度に合わせて更新する
    /// </summary>
    private void UpdateIndicatorRotation()
    {
        if (indicatorTransform == null) return;

        Quaternion baseRotation = Quaternion.Euler(0f, MoveDirection, 0f);
        Quaternion shotOffsetRotation = Quaternion.Euler(-basePitchAngle, 0f, 0f);

        indicatorTransform.rotation = baseRotation * shotOffsetRotation;
    }

    /// <summary>
    /// ボールを初期位置に移動させ、物理挙動を完全に静止・固定状態にする
    /// </summary>
    [ContextMenu("Reset Ball")]
    public void ResetBall()
    {
        if (targetRigidbody == null) return;

        // 1. Kinematicにする前に、速度と角速度をゼロにクリアする
        if (!targetRigidbody.isKinematic)
        {
            targetRigidbody.linearVelocity = Vector3.zero;
            targetRigidbody.angularVelocity = Vector3.zero;
        }

        // 2. 物理挙動をKinematic化して移動・重力をストップ
        // Unity 6でのプロパティ名変更に対応（旧 velocity / angularVelocity の代わり）
        targetRigidbody.isKinematic = true;

        // 3. 位置と回転を初期値へリセット（回転も基準向きに合わせておく）
        targetRigidbody.transform.position = initialPosition;
        targetRigidbody.transform.rotation = Quaternion.Euler(0f, MoveDirection, 0f);
    }

    /// <summary>
    /// 設定されたパラメータに基づいてボールを発射する
    /// </summary>
    [ContextMenu("Launch Ball")]
    public void LaunchBall()
    {
        if (targetRigidbody == null || !targetRigidbody.isKinematic) return;

        // 1. 物理運動を開始（Kinematicを先に解除する）
        targetRigidbody.isKinematic = false;

        // 2. Kinematic解除後に速度をクリア
        targetRigidbody.linearVelocity = Vector3.zero;
        targetRigidbody.angularVelocity = Vector3.zero;

        // --- 射出方向とパワーの計算処理 ---
        // indicatorTransform が割り当てられている場合はその Forward 方向、なければ計算して求める（安全対策）
        Vector3 launchDirection;
        if (indicatorTransform != null)
        {
            launchDirection = indicatorTransform.forward;
        }
        else
        {
            Quaternion baseRotation = Quaternion.Euler(0f, MoveDirection, 0f);
            Quaternion shotOffsetRotation = Quaternion.Euler(-basePitchAngle, 0f, 0f);
            launchDirection = (baseRotation * shotOffsetRotation) * Vector3.forward;
        }
        
        float calculatedPower = maxLaunchPower;

        // 3. 計算された力を加える
        targetRigidbody.AddForce(launchDirection * calculatedPower, forceMode);
    }
}