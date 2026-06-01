using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager _audioManager { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_audioManager == null)
		{
			_audioManager = new GameObject("_audioManager").AddComponent<AudioManager>();
			DontDestroyOnLoad(_audioManager.gameObject);
		}
		else
		{
			Destroy(_audioManager.gameObject);
			_audioManager = new GameObject("_audioManager").AddComponent<AudioManager>();
            		
        }
    }
        void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
