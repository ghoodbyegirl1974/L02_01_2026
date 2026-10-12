using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 【授業解説用】3タップ方式ゴルフショットUI制御クラス（元コードのロジックを完全維持した抽出版）
/// </summary>
public class GolfShotUI_test : MonoBehaviour
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
    [SerializeField] private RectTransform impactZone;    // インパクトエリア表示用
    [SerializeField] private RectTransform niceShotZone;  // ナイスショットエリア表示用
    [SerializeField] private RectTransform powerBarMask;  // RectMask2Dがついた親オブジェクトのRectTransform
    [SerializeField] private RectTransform powerBarImage; // Maskの中に配置した子ImageのRectTransform

    [Header("Gauge Settings")]
    [SerializeField] private float gaugeSpeed = 2.0f;     // ゲージ移動速度

    [Header("Impact Zone Settings")]
    [SerializeField] private float impactRatio = 0.9f;       // 0.9 の位置
    [SerializeField] private float impactZoneWidth = 0.1f;   // Zoneの幅設定
    [SerializeField] private float niceShotZoneWidth = 0.02f;// NiceShotZoneの幅設定

    [Header("Input System Settings")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string tapActionName = "Launch"; // PlayerInput上のショットアクション名

    private ShotState currentState = ShotState.Ready;
    private float cursorValue = 0.9f;  // 0.0 (左端: MAX) 〜 1.0 (右端)
    private float selectedPower = 0f;  // 決定されたパワー (0.0 〜 1.0)
    private float barWidth = 0f;
    private float barHeight = 0f;

    private InputAction tapAction;

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

    private void SetupPowerBarTransform()
    {
        if (powerBarMask == null || gaugeBar == null) return;

        powerBarMask.pivot = new Vector2(1.0f, 0.5f);
        powerBarMask.anchorMin = new Vector2(0.5f, 0.5f);
        powerBarMask.anchorMax = new Vector2(0.5f, 0.5f);

        float leftX = -barWidth * gaugeBar.pivot.x;
        float rightX = barWidth * (1f - gaugeBar.pivot.x);
        float impactXPos = Mathf.Lerp(leftX, rightX, impactRatio);

        powerBarMask.anchoredPosition = new Vector2(impactXPos, 0f);

        if (powerBarImage != null)
        {
            float maxPowerBarWidth = barWidth * impactRatio;
            powerBarImage.sizeDelta = new Vector2(maxPowerBarWidth, barHeight);

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
                cursorValue -= Time.deltaTime * gaugeSpeed;
                if (cursorValue <= 0.0f)
                {
                    cursorValue = 0.0f;
                    selectedPower = 1.0f;
                    currentState = ShotState.ImpactSelecting;
                }
                UpdateCursorPosition();
                UpdatePowerBarFill();
                break;

            case ShotState.ImpactSelecting:
                cursorValue += Time.deltaTime * gaugeSpeed;
                if (cursorValue >= 1.0f)
                {
                    cursorValue = 1.0f;
                    ExecuteShot();
                }
                UpdateCursorPosition();
                break;
        }
    }

    // =========================================================================
    // 【解説対象 1】スペースキー（タップ）入力によるステート遷移処理
    // =========================================================================
    public void OnTapInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        //L02_02 switch文記載
    }

    // =========================================================================
    // 【解説対象 2】cursorValueに応じたカーソル移動（描画）処理
    // =========================================================================
    private void UpdateCursorPosition()
    {
        //L02_02 Lerpによるカーソル位置の計算。
    }

    // =========================================================================
    // 【解説対象 3】打球の強さと左右角度変化量（YawRatio）を取得する処理
    // =========================================================================
    private void ExecuteShot()
    {
        //L02_02 最終的な数値計算。
    }

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

        float maxPowerBarWidth = barWidth * impactRatio;
        powerBarMask.sizeDelta = new Vector2(maxPowerBarWidth * powerRatio, barHeight);
    }

    private void UpdateImpactZoneGraphic()
    {
        if (gaugeBar == null) return;

        float leftX = -barWidth * gaugeBar.pivot.x;
        float rightX = barWidth * (1f - gaugeBar.pivot.x);
        float xPos = Mathf.Lerp(leftX, rightX, impactRatio);

        if (impactZone != null)
        {
            impactZone.anchoredPosition = new Vector2(xPos, impactZone.anchoredPosition.y);
        }

        if (niceShotZone != null)
        {
            niceShotZone.anchoredPosition = new Vector2(xPos, niceShotZone.anchoredPosition.y);
        }
    }

    public void ResetUI()
    {
        currentState = ShotState.Ready;
        cursorValue = impactRatio;
        selectedPower = 0f;

        InitializeGaugeDimensions();

        UpdateCursorPosition();
        UpdateImpactZoneGraphic();
        UpdatePowerBarFill();
    }
}