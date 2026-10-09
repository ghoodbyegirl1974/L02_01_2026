using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 現在の風の向き（矢印UI回転）と風力（テキスト表示）をリアルタイムで画面表示するクラス
/// </summary>
public class WindUI_comp : MonoBehaviour
{
    [Header("References")]
    [Tooltip("風情報を取得する WindManager_comp")]
    [SerializeField] private WindManager_comp windManager;

    [Tooltip("風向を示す矢印画像のRectTransform（デフォルトで上向き）")]
    [SerializeField] private RectTransform windArrowImage;

    [Tooltip("風力を表示する TextMeshProUGUI")]
    [SerializeField] private TextMeshProUGUI windForceText;

    [Header("Display Settings")]
    [Tooltip("windForceに掛ける表示用の係数")]
    [SerializeField] private float displayMultiplier = 1.0f;

    [Tooltip("風力の表示フォーマット（例: {0:F1} m/s）")]
    [SerializeField] private string textFormat = "{0:F1} m/s";

    private void Update()
    {
        UpdateWindUI();
    }

    /// <summary>
    /// WindManager_comp の最新状態を取得し、矢印の回転とテキストを更新する
    /// </summary>
    public void UpdateWindUI()
    {
        if (windManager == null) return;

        // 1. 風力テキストの更新
        if (windForceText != null)
        {
            float calculatedForce = windManager.CurrentWindForce * displayMultiplier;
            windForceText.text = string.Format(textFormat, calculatedForce);
        }

        // 2. 風向矢印の回転更新
        if (windArrowImage != null)
        {
            Vector3 windVec = windManager.CurrentWindVector;

            // X軸（左右）と Z軸（前後）から角度(度数法)を算出
            // Z軸正方向(0, 1)が0度（上向き）となります
            if (windVec.sqrMagnitude > 0.001f)
            {
                // Mathf.Atan2(x, z) で Z軸正方向を基準とした角度（ラジアン）を取得
                float angleRad = Mathf.Atan2(windVec.x, windVec.z);
                float angleDeg = angleRad * Mathf.Rad2Deg;

                // UIのZ軸回転は反時計回りが正となるため、-angleDeg を適用
                windArrowImage.localRotation = Quaternion.Euler(0f, 0f, -angleDeg);
            }
        }
    }
}