using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class GameLoop : MonoBehaviour
{
    public static event Action<float> OnTick;
    public static event Action OnGameOver;
    public static event Action OnPause;
    public static event Action OnResume;

    [Header("Game State")]
    [SerializeField] private bool startPaused = true;
    [SerializeField, Min(0f)] private float resumeDelaySeconds = 1f;

    [Header("UI (Gameplay sahnesinde)")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [Tooltip("Gameplay sahnesindeki skor TextMeshPro için tag (opsiyonel).")]
    [SerializeField] private string scoreTextTag = "ScoreText";
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private string highScoreTag = "HighScoreText";
    [SerializeField] private string pauseButtonTag = "PauseButton";
    [SerializeField] private Button pauseButton;
    [SerializeField] private string resumeButtonTag = "ResumeButton";
    [SerializeField] private Button resumeButton;
    [SerializeField] private GameObject pauseMenu;



    [Header("Perf / Platform")]
    [SerializeField] private int targetFrameRate = 60;

    // runtime state
    [SerializeField] private bool isPaused = false;
    [SerializeField] private bool isGameOver = false;

    private Coroutine scoreCoroutine;
    private Coroutine resumeRoutine;

    private int currentScore;
    private int highScore;
    private float scoreFrac;
    private float elapsedTime;

    public static GameLoop instance;
    public bool IsPaused => isPaused;
    public bool IsGameOver => isGameOver;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); return; }

        Application.targetFrameRate = targetFrameRate;

        currentScore = 0;
        scoreFrac = 0f;
        elapsedTime = 0f;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoadedRebind;
        EventManager.Subscribe("GameOver", GameOver);
        EventManager.Subscribe("Pause", Pause);
        EventManager.Subscribe("Resume", Resume);
        EventManager.Subscribe("ResetState", ResetState);

    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedRebind;
        EventManager.Unsubscribe("GameOver", GameOver);
        EventManager.Unsubscribe("Pause", Pause);
        EventManager.Unsubscribe("Resume", Resume);
        EventManager.Unsubscribe("ResetState", ResetState);

    }

    void Start()
    {
        //PlayerPrefs.DeleteKey("HighScore"); 
        highScore = PlayerPrefs.GetInt("HighScore", 0);

        TryAutoBindScoreText();
        TryAutoBindButtons(); // 🎯 Yeni satır

        UpdateScoreUI();

        if (startPaused) Pause();
        else Resume();

        StartScoreRoutineIfNeeded();

        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePause);
       
        pauseMenu.SetActive(false);
    }


    void Update()
    {
        if (!isPaused && !isGameOver)
            OnTick?.Invoke(Time.deltaTime);
    }

    // ---------- UI Binding ----------
    private void OnSceneLoadedRebind(Scene scene, LoadSceneMode mode)
    {
        TryAutoBindScoreText();   // Skor textlerini yeniden bul
        TryAutoBindButtons();     // 🎯 Pause butonunu yeniden bul
        UpdateScoreUI();

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(TogglePause);
        }
        if (resumeButton != null) 
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(TogglePause);
        }

        if (pauseMenu != null)
            pauseMenu.SetActive(false); // Menü gizle
        else
            pauseMenu = GameObject.Find("PauseMenu");
            pauseMenu.SetActive(false); // Menü gizle


    }

    void TryAutoBindScoreText()
    {
        if (!scoreText && !string.IsNullOrEmpty(scoreTextTag))
        {
            var go = GameObject.FindGameObjectWithTag(scoreTextTag);
            if (go) scoreText = go.GetComponent<TextMeshProUGUI>();
        }

        if (!highScoreText && !string.IsNullOrEmpty(highScoreTag))
        {
            var go = GameObject.FindGameObjectWithTag(highScoreTag);
            if (go) highScoreText = go.GetComponent<TextMeshProUGUI>();
        }
    }

    private void TryAutoBindButtons()
    {
        if (!pauseButton)
        {
            var go = GameObject.FindGameObjectWithTag("PauseButton");
            if (go) pauseButton = go.GetComponent<Button>();
        }
        if (!resumeButton)
        {
            var go = GameObject.FindGameObjectWithTag("ResumeButton");
            if (go) resumeButton = go.GetComponent<Button>();
        }
    }

    // ---------- Score ----------
    private void StartScoreRoutineIfNeeded()
    {
        if (scoreCoroutine == null)
            scoreCoroutine = StartCoroutine(UpdateScoreCoroutine());
    }

    private IEnumerator UpdateScoreCoroutine()
    {
        while (true)
        {
            if (isGameOver)
            {
                scoreCoroutine = null;
                yield break;
            }

            if (!isPaused)
            {
                float ratePerSecond = (elapsedTime < 15f) ? 40f : 90f;
                float dt = Time.deltaTime;
                elapsedTime += dt;
                scoreFrac += ratePerSecond * dt;

                int add = (int)scoreFrac; // floor
                if (add > 0)
                {
                    scoreFrac -= add;
                    currentScore += add;
                    UpdateScoreUI();
                }
            }
            yield return null;
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText)
            scoreText.text = currentScore.ToString();

        if (highScoreText)
            highScoreText.text = highScore.ToString();
    }


    // ---------- Flow ----------
    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        isPaused = true;

        if (resumeRoutine != null) { StopCoroutine(resumeRoutine); resumeRoutine = null; }
        if (scoreCoroutine != null) { StopCoroutine(scoreCoroutine); scoreCoroutine = null; }

        Time.timeScale = 0f;
        OnGameOver?.Invoke();

        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }
        UpdateScoreUI();
        // Menüye geçişi SceneDirector yapar
        var dir = FindObjectOfType<SceneDirector>();
        if (dir) dir.ShowMenu();
    }

    public void TogglePause()
    {
        if (isPaused && !isGameOver)
        {
            Resume();
            if (pauseMenu != null)
                pauseMenu.SetActive(false);
        }
        else if (!isPaused && !isGameOver)
        {
            Pause();
            if (pauseMenu != null)
                pauseMenu.SetActive(true);
        }
    }

    public void Pause()
    {
        if (isPaused) return;

        isPaused = true;

        if (resumeRoutine != null)
        {
            StopCoroutine(resumeRoutine);
            resumeRoutine = null;
        }

        Time.timeScale = 0f;
        OnPause?.Invoke();
    }
    /// <summary>Pause'dan oyuna dönüş. (SceneDirector Gameplay'i açtıktan sonra çağırmalı)</summary>
    public void Resume()
    {
        // Eğer zaten oynuyorsa ve game over değilse gerek yok
        if (!isPaused && !isGameOver) return;
        if (resumeRoutine != null) return;

        resumeRoutine = StartCoroutine(Co_Resume());
    }

    private IEnumerator Co_Resume()
    {

        // Beklerken iptal nedeni oluştu mu?
        if (isGameOver || !isPaused)
        {
            resumeRoutine = null;
            yield break;
        }

        isPaused = false;
        Time.timeScale = 1f;
        OnResume?.Invoke();

        if (scoreCoroutine == null)
            scoreCoroutine = StartCoroutine(UpdateScoreCoroutine());

        resumeRoutine = null;
    }

    /// <summary>
    /// Start butonu → SceneDirector.StartGame() + güvenli Resume.
    /// Bu çağrı **oyunu da** (gecikmeli) başlatır; SceneDirector Resume çağırsa da
    /// korumamız ikinci çağrıyı engeller.
    /// </summary>
    public void OnClickStart()
    {
        // skor & timer reset
        isGameOver = false;
        currentScore = 0;
        scoreFrac = 0f;
        elapsedTime = 0f;
        UpdateScoreUI();

        // 🎯 Event yay: SceneDirector bunu dinleyecek
        EventManager.Invoke("GameStartRequested");

        // sahne yüklense bile tekrar resume’lemeyi garantile
        Resume();
    }

    public void ResetState()
    {
        isGameOver = false;
    }

}
