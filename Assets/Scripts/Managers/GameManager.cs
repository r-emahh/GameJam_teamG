using UnityEngine;

public class GameManager : MonoBehaviour
{

    void initialize()
    {
        // 初期化処理をここでまとめて呼んでおく
        // バラバラにStartなどで呼ぶのではなく、ここでまとめて呼ぶことで、初期化の順番を管理しやすくなる


    }
    void Start()
    {
        initialize();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
