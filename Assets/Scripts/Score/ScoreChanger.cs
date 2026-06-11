using TMPro;
using UnityEngine;

public class ScoreChanger : MonoBehaviour
{
    //勝利プレイヤーを保存
    public int winnerPlayer;

    //Player1のスコアを保存
    public float score1P = 0;

    //Player2のスコアを保存
    public float score2P = 0;

    //Resultに表示するための変数
    public TextMeshProUGUI resultUIScore1P;
    public TextMeshProUGUI resultUIScore2P;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //リザルトのスコアを変更させる
        //score1PText.text = Mathf.CeilToInt(score1P).ToString();
        //score2PText.text = Mathf.CeilToInt(score2P).ToString();

    }

    // Update is called once per frame
    void Update()
    {

    }
}
