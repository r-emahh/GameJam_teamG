using UnityEngine;

public class TimerSystem : MonoBehaviour
{
    //タイマーの時間（秒）
    public float timerSec = 60;

    public bool timerStart;

    //for debugging
    private float _timeCount;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timerStart = true;

        //for debugging
        Debug.Log("残り時間：" + Mathf.CeilToInt(timerSec));
    }

    // Update is called once per frame
    void Update()
    {
        //タイマーがfalseなら"return"で関数をすぐに終わらせる
        if (!timerStart) return;

        if(timerSec > 0)
        {
            timerSec -= Time.deltaTime;

            //for debugging
            _timeCount += Time.deltaTime;
        }
        else if (timerSec <= 0)
        {
            timerSec = 0;

            timerStart = false;
        }

        //for debugging
        if (_timeCount >= 1)
        {
            Debug.Log("残り時間：" + Mathf.CeilToInt(timerSec));
            _timeCount = 0;
        }
    }
}
