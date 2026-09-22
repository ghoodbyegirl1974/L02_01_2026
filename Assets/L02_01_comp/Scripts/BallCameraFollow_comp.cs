using UnityEngine;

/// <summary>
/// ボール（ターゲット）を一定のオフセットで追随するサブカメラ制御クラス
/// </summary>
public class BallCameraFollow_comp : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("追随対象のオブジェクト（ボールなど）")]
    [SerializeField] private GameObject target;

    [Tooltip("ターゲットからの相対位置（カメラの位置調整）")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 3f, -5f);

    void Start(){
        offset = transform.position;
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.transform.position + offset;
        }
    }
}