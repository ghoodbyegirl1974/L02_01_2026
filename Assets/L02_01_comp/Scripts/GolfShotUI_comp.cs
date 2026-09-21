using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// みんゴル風 3タップ方式ゴルフショットUI制御クラス（RectMask2Dによるパワーゲージ表示対応版）
/// </summary>
public class GolfShotUI_comp : MonoBehaviour
{
    public enum ShotState
    {
        Ready,          // 発射準備中（1タップ目待ち）
        PowerSelecting, // パワー選択中（左移動: 右側 0.9 ➔ 左端 0.0）
        ImpactSelecting,// インパクト選択中（右移動: 左端 0.0 ➔ 右側 1.0方向）
        Executed        // ショット実行済み
    }

    [Header("UI References")]
    [SerializeField] private RectTransform gaugeBar;      // ゲージ背景のRectTransform
    [SerializeField] private RectTransform gaugeCursor;   // ゲージカーソルのRectTransform
    [SerializeField] private RectTransform impactZone;    // ジャストインパクトエリア表示用
    [SerializeField] private RectTransform powerBarMask;  // ★RectMask2Dがついた親オブジェクトのRectTransform
    [SerializeField] private RectTransform powerBarImage; // ★Maskの中に配置した子ImageのRectTransform
    [SerializeField] private TextMeshProUGUI statusText;  // 状態表示テキスト

    [Header("Shot Target Settings")]
    [SerializeField] private BallLauncher_comp ballLauncher;
    [SerializeField] private float maxLaunchPower = 150f; // 最大パワー
    [SerializeField] private float basePitchAngle = 30f;  // 基本打ち出し角
    [SerializeField] private float maxMissYawAngle = 15f; // インパクト失敗時の最大左右ズレ角度

    [Header("Gauge Settings")]
    [SerializeField] private float gaugeSpeed = 2.0f;     // ゲージ移動速度

    [Header("Impact Zone Settings")]
    [Tooltip("ジャストインパクトおよび初期開始位置の比率 (0.9 = 右から10%の位置)")]
    [Range(0.7f, 0.95f)]
    [SerializeField] private float impactRatio = 0.9f;    // 0.9 の位置

    [Header("Input System Settings")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string tapActionName = "Launch"; // PlayerInput上のアクション名

    private ShotState currentState = ShotState.Ready;
    private float cursorValue = 0.9f;   // 0.0 (左端: MAX) 〜 1.0 (右端)
    private float selectedPower = 0f;  // 決定されたパワー (0.0 〜 1.0)
    private float selectedImpact = 0f; // 決定されたインパクトのズレ
    private float barWidth = 0f;
    private float barHeight = 0f;
    private InputAction tapAction;

    // 連続タップ誤動作防止用の変数
    private float lastTapTime = 0f;
    private const float tapCooldown = 0.15f; // 150ミリ秒以内の連打・連続イベントをガード

    private void Awake()
    {
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        if (playerInput != null && playerInput.actions != null)
        {
            tapAction = playerInput.actions.FindAction(tapActionName);
            if (tapAction != null)
            {
                tapAction.performed += OnTapInput;
                tapAction.Enable();
            }
        }
    }

    private void OnDisable()
    {
        if (tapAction != null)
        {
            tapAction.performed -= OnTapInput;
        }
    }

    private void Start()
    {
        InitializeGaugeDimensions();
        ResetUI();
    }

    private void InitializeGaugeDimensions()
    {
        if (gaugeBar != null)
        {
            barWidth = gaugeBar.rect.width;
            barHeight = gaugeBar.rect.height;
            SetupPowerBarTransform();
        }
    }

    /// <summary>
    /// ★powerBarMask と powerBarImage のサイズ・位置関係をプログラミングで初期セットアップ
    /// </summary>
    private void SetupPowerBarTransform()
    {
        if (powerBarMask == null || gaugeBar == null) return;

        // --- 1. 親（powerBarMask）の設定 ---
        // Pivotを右中央(1.0, 0.5)にして、右端から左へマスク領域が伸び縮みするように設定
        powerBarMask.pivot = new Vector2(1.0f, 0.5f);
        powerBarMask.anchorMin = new Vector2(0.5f, 0.5f);
        powerBarMask.anchorMax = new Vector2(0.5f, 0.5f);

        // インパクト位置（右側のスタート地点）のX座標を計算
        float leftX = -barWidth * gaugeBar.pivot.x;
        float rightX = barWidth * (1f - gaugeBar.pivot.x);
        float impactXPos = Mathf.Lerp(leftX, rightX, impactRatio);

        // マスク自体の基準位置（右端＝インパクト位置）
        powerBarMask.anchoredPosition = new Vector2(impactXPos, 0f);

        // --- 2. 子（powerBarImage）の設定 ---
        if (powerBarImage != null)
        {
            // 子Imageのサイズは「最大パワー時の固定サイズ（引き伸ばさないサイズ）」に固定
            float maxPowerBarWidth = barWidth * impactRatio;
            powerBarImage.sizeDelta = new Vector2(maxPowerBarWidth, barHeight);

            // 子ImageのPivotも右中央(1.0, 0.5)にし、親の右端に基準位置を合わせる
            powerBarImage.pivot = new Vector2(1.0f, 0.5f);
            powerBarImage.anchorMin = new Vector2(1.0f, 0.5f);
            powerBarImage.anchorMax = new Vector2(1.0f, 0.5f);
            powerBarImage.anchoredPosition = Vector2.zero;
        }
    }

    private void Update()
    {
        switch (currentState)
        {
            case ShotState.PowerSelecting:
                // 右(0.9)から左(0.0)に向かって移動（パワー増加）
                cursorValue -= Time.deltaTime * gaugeSpeed;
                if (cursorValue <= 0.0f)
                {
                    cursorValue = 0.0f;
                    selectedPower = 1.0f; // 左端超過時は自動折り返し（100%確定）
                    currentState = ShotState.ImpactSelecting;
                    lastTapTime = Time.time;
                }
                UpdateCursorPosition();
                UpdatePowerBarFill();
                break;

            case ShotState.ImpactSelecting:
                // 左(0.0)から右(1.0)に向かって移動（インパクト合わせ）
                cursorValue += Time.deltaTime * gaugeSpeed;
                if (cursorValue >= 1.0f)
                {
                    cursorValue = 1.0f;
                    ExecuteShotWithMiss();
                }
                UpdateCursorPosition();
                break;
        }
    }

    public void OnTapInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (Time.time - lastTapTime < tapCooldown) return;
        lastTapTime = Time.time;

        switch (currentState)
        {
            case ShotState.Ready:
                currentState = ShotState.PowerSelecting;
                cursorValue = impactRatio;
                UpdateStatusText("Power Selecting... Press Space to Lock Power!");
                break;

            case ShotState.PowerSelecting:
                selectedPower = Mathf.Clamp01((impactRatio - cursorValue) / impactRatio);
                currentState = ShotState.ImpactSelecting;
                UpdatePowerBarFill();
                UpdateStatusText($"Power: {Mathf.RoundToInt(selectedPower * 100)}% | Select Impact!");
                break;

            case ShotState.ImpactSelecting:
                float missDistance = Mathf.Abs(cursorValue - impactRatio);
                selectedImpact = missDistance / impactRatio;
                ExecuteShot();
                break;
        }
    }

    private void ExecuteShot()
    {
        currentState = ShotState.Executed;

        float finalPower = selectedPower * maxLaunchPower;
        float missRatio = Mathf.Clamp01(selectedImpact);
        float yawOffset = (Random.value > 0.5f ? 1f : -1f) * missRatio * maxMissYawAngle;

        string impactMsg = "NICE SHOT!!";
        if (missRatio > 0.15f) impactMsg = "BAD SHOT!";
        else if (missRatio > 0.05f) impactMsg = "GOOD SHOT";

        UpdateStatusText($"{impactMsg} (Power: {Mathf.RoundToInt(selectedPower * 100)}%)");

        if (ballLauncher != null)
        {
            ballLauncher.SetShotParameters(finalPower, basePitchAngle, yawOffset);
            ballLauncher.LaunchBall();
        }
    }

    private void ExecuteShotWithMiss()
    {
        selectedImpact = 1.0f;
        ExecuteShot();
    }

    public void ResetUI()
    {
        currentState = ShotState.Ready;
        cursorValue = impactRatio;
        selectedPower = 0f;
        selectedImpact = 0f;
        lastTapTime = 0f;

        InitializeGaugeDimensions();

        UpdateCursorPosition();
        UpdateImpactZoneGraphic();
        UpdatePowerBarFill();
        UpdateStatusText("Press Space to Start Shot");
    }

    private void UpdateCursorPosition()
    {
        if (gaugeCursor != null && gaugeBar != null)
        {
            float leftX = -barWidth * gaugeBar.pivot.x;
            float rightX = barWidth * (1f - gaugeBar.pivot.x);

            float xPos = Mathf.Lerp(leftX, rightX, cursorValue);
            gaugeCursor.anchoredPosition = new Vector2(xPos, gaugeCursor.anchoredPosition.y);
        }
    }

    /// <summary>
    /// ★Mask（親）の幅だけを伸縮させてパワーゲージの表示範囲を更新する
    /// </summary>
    private void UpdatePowerBarFill()
    {
        if (powerBarMask == null) return;

        float powerRatio = 0f;

        if (currentState == ShotState.PowerSelecting)
        {
            powerRatio = Mathf.Clamp01((impactRatio - cursorValue) / impactRatio);
        }
        else if (currentState == ShotState.ImpactSelecting || currentState == ShotState.Executed)
        {
            powerRatio = selectedPower;
        }

        // 最大幅 × パワー割合 でマスクの幅を設定（高さを維持し、横幅だけ変化）
        float maxPowerBarWidth = barWidth * impactRatio;
        powerBarMask.sizeDelta = new Vector2(maxPowerBarWidth * powerRatio, barHeight);
    }

    private void UpdateImpactZoneGraphic()
    {
        if (impactZone != null && gaugeBar != null)
        {
            float leftX = -barWidth * gaugeBar.pivot.x;
            float rightX = barWidth * (1f - gaugeBar.pivot.x);

            float xPos = Mathf.Lerp(leftX, rightX, impactRatio);
            impactZone.anchoredPosition = new Vector2(xPos, impactZone.anchoredPosition.y);
        }
    }

    private void UpdateStatusText(string msg)
    {
        if (statusText != null)
        {
            statusText.text = msg;
        }
    }
}