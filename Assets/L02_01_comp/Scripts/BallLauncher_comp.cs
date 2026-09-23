using UnityEngine;

public class BallLauncher_comp : MonoBehaviour
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

    [Header("Impact Yaw Settings")]
    [Tooltip("ImpactZone内の端ギリギリで発生する最大ブレ角度（度）")]
    [SerializeField] private float maxZoneYawAngle = 10f;

    [Tooltip("ImpactZoneを完全に外した（ミス）際の最大左右ズレ角度（度）")]
    [SerializeField] private float maxMissYawAngle = 25f;

    [Header("Force Option")]
    [SerializeField] private ForceMode forceMode = ForceMode.Impulse;

    [Header("References")]
    [Tooltip("飛距離計測コンポーネントへの参照")]
    [SerializeField] private DistanceCalculator_comp distanceCalculator;

    private void Awake()
    {
        if (distanceCalculator == null && targetRigidbody != null)
        {
            distanceCalculator = targetRigidbody.GetComponent<DistanceCalculator_comp>();
        }
    }

    private void Start()
    {
        ResetBall();
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
        targetRigidbody.isKinematic = true;

        // 3. 位置と回転を初期値へリセット（回転も基準向きに合わせておく）
        targetRigidbody.transform.position = initialPosition;
        targetRigidbody.transform.rotation = Quaternion.Euler(0f, MoveDirection, 0f);

        // 4. 飛距離計測コンポーネントへのリセット通知
        if (distanceCalculator != null)
        {
            distanceCalculator.ResetMeasurement();
        }
    }

    /// <summary>
    /// 設定されたパラメータおよびUIから与えられた比率・判定に基づいてボールを発射する
    /// </summary>
    /// <param name="powerRatio">パワーの比率 (0.0 ～ 1.0)</param>
    /// <param name="yawRatio">左右のブレ比率 (-1.0 ～ +1.0)</param>
    /// <param name="isImpactZone">ImpactZone内でのショットかどうか</param>
    public void LaunchBall(float powerRatio, float yawRatio, bool isImpactZone)
    {
        if (targetRigidbody == null || !targetRigidbody.isKinematic) return;

        // 1. 物理運動を開始（Kinematicを先に解除する）
        targetRigidbody.isKinematic = false;

        // 2. Kinematic解除後に速度をクリア
        targetRigidbody.linearVelocity = Vector3.zero;
        targetRigidbody.angularVelocity = Vector3.zero;

        // 3. インパクト判定に応じた最大ブレ角度を適用し、最終的なヨーオフセット角度を計算
        float appliedMaxYaw = isImpactZone ? maxZoneYawAngle : maxMissYawAngle;
        float finalYawOffset = Mathf.Clamp(yawRatio, -1.0f, 1.0f) * appliedMaxYaw;

        // 4. 基準向き（MoveDirection）とショットのピッチ・ヨー角度を合成して発射方向を計算
        Quaternion baseRotation = Quaternion.Euler(0f, MoveDirection, 0f);
        Quaternion shotOffsetRotation = Quaternion.Euler(-basePitchAngle, finalYawOffset, 0f);

        Vector3 launchDirection = (baseRotation * shotOffsetRotation) * Vector3.forward;

        // 5. 計算された力（最大パワー × パワー比率）を加える
        float calculatedPower = maxLaunchPower * Mathf.Clamp01(powerRatio);
        targetRigidbody.AddForce(launchDirection * calculatedPower, forceMode);

        // 6. 飛距離計測コンポーネントへの開始通知
        if (distanceCalculator != null)
        {
            distanceCalculator.StartMeasurement();
        }
    }

    /// <summary>
    /// 単体テスト用（100%パワー、ブレなしで発射）
    /// </summary>
    [ContextMenu("Launch Ball")]
    public void LaunchBall()
    {
        LaunchBall(1.0f, 0f, true);
    }
}