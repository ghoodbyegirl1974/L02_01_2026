using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// これまでのショット結果（カップまでの距離）を蓄積し、今回の結果が最小値（自己ベスト）であるかを判定するクラス
/// </summary>
public class ResultManager_comp : MonoBehaviour
{
    // これまでの結果を蓄積するリスト
    private List<float> resultList = new List<float>();

    // ◆機能2: Start関数実行時に初期データ（10個）を追加
    private void Start()
    {
        float[] initialData = new float[] {50f, 55f, 60f, 65f, 70f, 75f, 80f, 85f, 90f, 95f, 100f };
        resultList.AddRange(initialData);
    }

    /// <summary>
    /// 今回の結果とこれまでに蓄積された全結果を比較チェックし、最小値であれば true を返します。
    /// また、今回の結果をリストに追加して蓄積します。
    /// </summary>
    /// <param name="currentResult">今回のショット結果（カップまでの距離）</param>
    /// <returns>今回の結果がこれまでの最小値であれば true、そうでなければ false</returns>
    public bool CheckResult(float currentResult)
    {
        bool isNewBest = true;
        Debug.Log("currentResult = " + currentResult.ToString());
        // これまでに蓄積された結果全てと突き合わせて比較
        foreach (float result in resultList)
        {
            if (currentResult >= result)
            {
                isNewBest = false;
                break;
            }
        }

        // 今回の結果をリストに追加して蓄積
        resultList.Add(currentResult);

        // ◆機能1: 昇順（数が少ない順）に並べ直し、10個を超える要素（11番目以降）を削除
        resultList.Sort();
        if (resultList.Count > 10)
        {
            resultList.RemoveRange(10, resultList.Count - 10);
        }

        return isNewBest;
    }
}