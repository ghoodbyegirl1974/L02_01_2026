using UnityEngine;
using TMPro;

/// <summary>
/// ボールの飛距離（メートル換算）およびカップまでの距離を計測・表示するコンポーネント
/// </summary>
public class DistanceCalculator_comp : MonoBehaviour
{
    public enum DistanceMode
    {
        CarryOnly,     // 最初に地面についた時点（キャリー）で確定
        UntilStop      // 完全停止する時点（トータル）まで継続計測
    }

    [Header("Target Settings")]
    [Tooltip("計測対象のボールのRigidbody")]
    [SerializeField] private Rigidbody targetRigidbody;

    [Tooltip("目標となるカップのGameObject")]
    [SerializeField] private GameObject cupObject;

    [Header("Calculation Mode")]
    [Tooltip("飛距離の計測完了タイミング")]
    [SerializeField] private DistanceMode distanceMode = DistanceMode.CarryOnly;

    [Header("Scale Settings")]
    [Tooltip("Unityの空間スケール（10単位 = 1m の場合は 10 を設定）")]
    [SerializeField] private float unitsPerMeter = 10f;

    [Header("Ground / Land Settings")]
    [Tooltip("地面と判定するレイヤー")]
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Stop Detection Settings")]
    [Tooltip("停止とみなす速度のしきい値")]
    [SerializeField] private float stopVelocityThreshold = 0.1f;

    [Tooltip("停止状態がこの時間(秒)続いたら完全に停止したと判断する")]
    [SerializeField] private float stopDurationThreshold = 0.5f;

    [Header("UI Settings (Optional)")]
    [Tooltip("飛距離を表示するTextMeshProUGUI（任意）")]
    [SerializeField] private TextMeshProUGUI distanceText;

    [Tooltip("カップまでの距離を表示するTextMeshProUGUI（任意）")]
    [SerializeField] private TextMeshProUGUI distanceToCupText;

    // --- 【外部参照用プロパティ】 ---
    /// <summary>
    /// 確定したカップまでの水平距離（メートル）。未確定時は -1 を返します。
    /// </summary>
    public float DistanceToCup { get; private set; } = -1f;

    private Vector3 launchPosition;
    private bool isFlying = false;
    private bool isMeasurementComplete = false;
    private bool hasTouchedGround = false;
    private float stopTimer = 0f;

    private void Update()
    {
        if (!isFlying || isMeasurementComplete || targetRigidbody == null) return;

        float currentDistance = CalculateHorizontalDistance(launchPosition, targetRigidbody.position);

        string modeLabel = (distanceMode == DistanceMode.CarryOnly) ? "Flight" : "Rolling";
        UpdateUI($"{modeLabel}: {currentDistance:F1} m");

        if (distanceMode == DistanceMode.UntilStop && hasTouchedGround)
        {
            CheckBallStop(currentDistance);
        }
    }

    /// <summary>
    /// ボール発射時に呼び出す（計測開始）
    /// PlayerInputの自動SendMessagesとの衝突を防ぐため名称を変更しています
    /// </summary>
    public void StartMeasurement()
    {
        if (targetRigidbody == null) return;

        launchPosition = targetRigidbody.position;
        isFlying = true;
        isMeasurementComplete = false;
        hasTouchedGround = false;
        stopTimer = 0f;
        DistanceToCup = -1f; // 初期化

        UpdateUI("Flight: 0.0 m");
        
        // 計測開始時はカップ距離表示を非表示にする
        SetCupDistanceUIVisibility(false);

        Debug.Log($"[DistanceCalculator] 計測開始 (モード: {distanceMode}) - 発射位置: {launchPosition}");
    }

    /// <summary>
    /// ボールリセット時に呼び出す（状態クリア）
    /// </summary>
    public void ResetMeasurement()
    {
        isFlying = false;
        isMeasurementComplete = false;
        hasTouchedGround = false;
        stopTimer = 0f;
        DistanceToCup = -1f; // 初期化

        UpdateUI("Ready");

        // リセット時もカップ距離表示を非表示にする
        SetCupDistanceUIVisibility(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isFlying || isMeasurementComplete) return;

        if ((groundLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            hasTouchedGround = true;

            if (distanceMode == DistanceMode.CarryOnly)
            {
                Vector3 landPosition = collision.contacts[0].point;
                float carryDistance = CalculateHorizontalDistance(launchPosition, landPosition);

                isMeasurementComplete = true;
                Debug.Log($"<color=yellow>[DistanceCalculator] 着地確定 (Carry): {carryDistance:F2} m</color>");
                UpdateUI($"Carry: {carryDistance:F1} m");

                // カップまでの水平距離を測定・確定（表示も有効化）
                FinalizeCupDistance(landPosition);
            }
        }
    }

    private void CheckBallStop(float currentDistance)
    {
        if (targetRigidbody.linearVelocity.sqrMagnitude < stopVelocityThreshold * stopVelocityThreshold &&
            targetRigidbody.angularVelocity.sqrMagnitude < stopVelocityThreshold * stopVelocityThreshold)
        {
            stopTimer += Time.deltaTime;

            if (stopTimer >= stopDurationThreshold)
            {
                isMeasurementComplete = true;
                Debug.Log($"<color=green>[DistanceCalculator] 完全停止確定 (Total): {currentDistance:F2} m</color>");
                UpdateUI($"Total: {currentDistance:F1} m");

                // カップまでの水平距離を測定・確定（表示も有効化）
                FinalizeCupDistance(targetRigidbody.position);
            }
        }
        else
        {
            stopTimer = 0f;
        }
    }

    /// <summary>
    /// 距離確定時にカップとボールの最終位置間の水平距離を計算し、プロパティ保持・UI表示を行う
    /// </summary>
    private void FinalizeCupDistance(Vector3 ballPosition)
    {
        if (cupObject != null)
        {
            DistanceToCup = CalculateHorizontalDistance(cupObject.transform.position, ballPosition);
            
            // 距離確定時にテキストを設定して表示を有効化
            UpdateCupDistanceText($"Pin: {DistanceToCup:F1} m");
            SetCupDistanceUIVisibility(true);

            Debug.Log($"<color=cyan>[DistanceCalculator] カップまでの距離確定: {DistanceToCup:F2} m</color>");
        }
        else
        {
            Debug.LogWarning("[DistanceCalculator] cupObject が設定されていないため、カップまでの距離を計算できません。");
        }
    }

    private float CalculateHorizontalDistance(Vector3 start, Vector3 end)
    {
        Vector3 startXZ = new Vector3(start.x, 0f, start.z);
        Vector3 endXZ = new Vector3(end.x, 0f, end.z);

        float unityDistance = Vector3.Distance(startXZ, endXZ);
        return unityDistance / unitsPerMeter;
    }

    private void UpdateUI(string message)
    {
        if (distanceText != null)
        {
            distanceText.text = message;
        }
    }

    private void UpdateCupDistanceText(string message)
    {
        if (distanceToCupText != null)
        {
            distanceToCupText.text = message;
        }
    }

    /// <summary>
    /// distanceToCupText の表示・非表示を切り替える
    /// </summary>
    private void SetCupDistanceUIVisibility(bool visible)
    {
        if (distanceToCupText != null)
        {
            distanceToCupText.gameObject.SetActive(visible);
        }
    }
}