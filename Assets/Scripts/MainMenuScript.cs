using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuScript : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    private string nextSceneName = "Game"; // Set this in the Inspector
    public RawImage videoImage;
    public bool canSkip = false;

    private bool videoStarted = false;

    void Start()
    {
        videoPlayer.gameObject.SetActive(false); // Hide until triggered
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    void StartImage()
    {
        videoImage.color = new Color(1, 1, 1, 1);
        canSkip = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !videoStarted)
        {
            videoStarted = true;
            videoPlayer.gameObject.SetActive(true);
            videoPlayer.Play();
            Invoke("StartImage", 0.2f);
        }
        
        if (canSkip && Input.GetKeyDown(KeyCode.Space))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        SceneManager.LoadScene(nextSceneName);
    }
}