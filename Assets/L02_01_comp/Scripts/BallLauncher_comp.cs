using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class BallLauncher_comp : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Rigidbody targetRigidbody;
    [SerializeField] private Vector3 initialPosition = new Vector3(0f, 15f, 0f);

    [Header("Launch Parameters (10x Scale)")]
    [SerializeField] private float launchPower = 100f;
    [Range(-90f, 90f)] [SerializeField] private float yawAngle = 0f;
    [Range(0f, 90f)] [SerializeField] private float pitchAngle = 30f;

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

        // 3. 位置と回転を初期値へリセット
        targetRigidbody.transform.position = initialPosition;
        targetRigidbody.transform.rotation = Quaternion.identity;

        // 4. 飛距離計測コンポーネントへのリセット通知
        if (distanceCalculator != null)
        {
            distanceCalculator.ResetMeasurement();
        }
    }

    /// <summary>
    /// 設定された角度と力に基づいてボールを発射する
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

        // 3. 角度から発射方向を計算
        Quaternion launchRotation = Quaternion.Euler(-pitchAngle, yawAngle, 0f);
        Vector3 launchDirection = launchRotation * Vector3.forward;

        // 4. 力を加える
        targetRigidbody.AddForce(launchDirection * launchPower, forceMode);

        // 5. 飛距離計測コンポーネントへの開始通知
        if (distanceCalculator != null)
        {
            distanceCalculator.StartMeasurement();
        }
    }

    /// <summary>
    /// ゴルフUI側からショットのパワーと角度をセットする
    /// </summary>
    public void SetShotParameters(float power, float pitch, float yaw)
    {
        this.launchPower = power;
        this.pitchAngle = pitch;
        this.yawAngle = yaw;
    }
}