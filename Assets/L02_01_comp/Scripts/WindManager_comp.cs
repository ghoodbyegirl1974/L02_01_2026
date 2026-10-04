using UnityEngine;

/// <summary>
/// ティーショット後のボールに風の影響を与える管理クラス
/// </summary>
public class WindManager_comp : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("trueの場合、インスペクターで設定した固定値を使用します。falseの場合、ランダム生成されます")]
    [SerializeField] private bool isDebug = false;

    [Header("Wind Parameters")]
    [Tooltip("風の強さ（IsDebugがtrueの時に使用されます）")]
    [SerializeField] private float windForce = 5f;

    [Tooltip("風の向きの回転角度（度数法：0〜360度。Vector3.forwardを基準としY軸回転）")]
    [SerializeField] private float windAngle = 0f;

    [Header("Random Wind Settings")]
    [Tooltip("風の強さの最大値（0 〜 maxWindForce の範囲で乱数生成）")]
    [SerializeField] private float maxWindForce = 10f;

    // --- 【追加】風の影響を受ける最小高度設定 ---
    [Header("Height Limit Settings")]
    [Tooltip("このY座標（高さ）よりボールが高いときだけ風の影響を受けます")]
    [SerializeField] private float minWindHeight = 0f;

    [Header("Target Ball Reference")]
    [Tooltip("風の影響を受けるボールのRigidbody")]
    [SerializeField] private Rigidbody ballRigidbody;

    // --- 内部保持用プロパティ ---
    public Vector3 CurrentWindVector { get; private set; }
    public float CurrentWindForce { get; private set; }
    public float CurrentWindAngle { get; private set; }

    public bool IsDebug => isDebug;
    public float MinWindHeight => minWindHeight;

    private void Start()
    {
        GenerateWind();
    }

    private void FixedUpdate()
    {
        // ボールが存在し、Kinematic（停止固定状態）でなく、
        // かつボールのY座標が minWindHeight より高いときだけ風力を加える
        if (ballRigidbody != null && !ballRigidbody.isKinematic)
        {
            if (ballRigidbody.position.y > minWindHeight)
            {
                ballRigidbody.AddForce(CurrentWindVector, ForceMode.Force);
            }
        }
    }

    /// <summary>
    /// 風のパラメータ（強さ・角度・ベクトル）を決定・生成する
    /// </summary>
    [ContextMenu("Generate Wind")]
    public void GenerateWind()
    {
        if (isDebug)
        {
            // Debugモード時はインスペクターの設定値をそのまま使用
            CurrentWindForce = windForce;
            CurrentWindAngle = windAngle;
        }
        else
        {
            // 通常時は指定された範囲でランダム生成
            CurrentWindForce = Random.Range(0f, maxWindForce);
            CurrentWindAngle = Random.Range(0f, 360f);
        }

        // Vector3.forward を基準に Y軸周りで回転させた風の方向ベクトルを計算
        Quaternion windRotation = Quaternion.Euler(0f, CurrentWindAngle, 0f);
        Vector3 windDirection = windRotation * Vector3.forward;

        // 最終的な風力ベクトル（方向 × 強さ）を算出
        CurrentWindVector = windDirection * CurrentWindForce;

        Debug.Log($"[WindManager_comp] 風生成完了 - 強さ: {CurrentWindForce:F2}, 角度: {CurrentWindAngle:F1}度, ベクトル: {CurrentWindVector}");
    }
}