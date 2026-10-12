using UnityEngine;

[ExecuteAlways]
public class TextureTilingScaler_comp : MonoBehaviour
{
    private Renderer rend;
    private MaterialPropertyBlock propBlock;
    private Vector3 lastScale;
    
    [Tooltip("Scale.xが1のときのタイリング数 (X方向)")]
    public float baseTilingX = 1f;

    [Tooltip("Scale.zが1のときのタイリング数 (Y方向)")]
    public float baseTilingY = 1f;

    void Start()
    {
        rend = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();
        UpdateTiling();
    }

    void Update()
    {
        if (transform.localScale != lastScale)
        {
            UpdateTiling();
        }
    }

    void UpdateTiling()
    {
        if (rend == null) return;
        lastScale = transform.localScale;

        // 現在のレンダラーからプロパティブロックを取得
        rend.GetPropertyBlock(propBlock);

        // Scale.x と Scale.z の値に応じてタイリングを計算
        float newTilingX = baseTilingX * lastScale.x;
        float newTilingY = baseTilingY * lastScale.z; // Scale.z をテクスチャの Y(V) 方向に使用

        // URP (BaseMap) や 標準シェーダー (_MainTex) のスケール・オフセットを取得
        Vector4 st = propBlock.GetVector("_BaseMap_ST");
        if (st == Vector4.zero) st = new Vector4(1, 1, 0, 0); // 初期値のフォールバック
        
        // タイリング値を更新
        st.x = newTilingX;
        st.y = newTilingY;
        
        propBlock.SetVector("_BaseMap_ST", st);

        // 変更を適用
        rend.SetPropertyBlock(propBlock);
    }
}