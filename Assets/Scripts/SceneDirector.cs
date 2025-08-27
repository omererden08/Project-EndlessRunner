using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneDirector : MonoBehaviour
{
    [Header("Scene Names (Build Settings'te ekli olmalı)")]
    [SerializeField] private string menuSceneName = "StartMenu";
    [SerializeField] private string gameplaySceneName = "Gameplay";

    [Header("Flow")]
    [SerializeField, Min(0f)] private float startResumeDelay = 1f;
    [SerializeField] private bool loadBothOnBoot = true;

    public enum Mode { Menu, Gameplay }
    public Mode CurrentMode { get; private set; } = Mode.Menu;

    // Cache
    private Scene _menuScene;
    private Scene _gameScene;
    private readonly List<GameObject> _menuRoots = new List<GameObject>(32);
    private readonly List<GameObject> _gameRoots = new List<GameObject>(64);

    private Coroutine _switchRoutine;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    IEnumerator Start()
    {
        if (loadBothOnBoot)
        {
            // Menü sahnesini yükle
            yield return LoadIfNeeded(menuSceneName, _menuRoots, s => _menuScene = s);
            // Gameplay sahnesini yükle
            yield return LoadIfNeeded(gameplaySceneName, _gameRoots, s => _gameScene = s);

            // Başlangıçta sadece menü aktif
            SetRootsActive(_gameRoots, false);
            SetRootsActive(_menuRoots, true);

            SceneManager.SetActiveScene(_menuScene);
            GameLoop.instance?.Pause();
        }
    }


    void OnEnable()
    {
        EventManager.Subscribe("GameStartRequested", StartOrRestartGame);
    }

    void OnDisable()
    {
        EventManager.Unsubscribe("GameStartRequested", StartOrRestartGame);
    }



    // --- Public API ---

    public void ShowMenu()
    {
        if (_switchRoutine != null) StopCoroutine(_switchRoutine);
        _switchRoutine = StartCoroutine(Co_ShowMenu());
    }

    public void StartOrRestartGame()
    {
        if (_switchRoutine != null) StopCoroutine(_switchRoutine);
        _switchRoutine = StartCoroutine(Co_StartOrRestartGame());
    }

    // --- Coroutines ---

    private IEnumerator Co_ShowMenu()
    {
        if (!_menuScene.IsValid() || !_menuScene.isLoaded)
            yield return LoadIfNeeded(menuSceneName, _menuRoots, s => _menuScene = s);

        SetRootsActive(_gameRoots, false);
        SetRootsActive(_menuRoots, true);

        SceneManager.SetActiveScene(_menuScene);
        GameLoop.instance?.Pause();

        CurrentMode = Mode.Menu;
        _switchRoutine = null;
    }

    private IEnumerator Co_StartOrRestartGame()
    {
        ObjectPooler.Instance?.ReturnAllToPool();
        GameLoop.instance?.Pause();

        if (_gameScene.IsValid() && _gameScene.isLoaded)
        {
            SetRootsActive(_gameRoots, false);
            yield return SceneManager.UnloadSceneAsync(_gameScene);

            _gameRoots.Clear();
            _gameScene = default;

            yield return Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        yield return LoadIfNeeded(gameplaySceneName, _gameRoots, s => _gameScene = s);

        SetRootsActive(_menuRoots, false);
        SetRootsActive(_gameRoots, true);
        SceneManager.SetActiveScene(_gameScene);

        if (startResumeDelay > 0f)
            yield return new WaitForSecondsRealtime(startResumeDelay);

        GameLoop.instance?.ResetState();
        GameLoop.instance?.Resume();

        CurrentMode = Mode.Gameplay;
        _switchRoutine = null;
    }


    // --- Helpers ---

    private IEnumerator LoadIfNeeded(string sceneName, List<GameObject> cache, System.Action<Scene> assign)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        bool loadedNow = false;

        if (!scene.IsValid() || !scene.isLoaded)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!op.isDone) yield return null;

            scene = SceneManager.GetSceneByName(sceneName);
            loadedNow = true;
        }

        assign?.Invoke(scene);
        CacheSceneRoots(scene, cache);
    }

    private void CacheSceneRoots(Scene scene, List<GameObject> cache)
    {
        cache.Clear();
        scene.GetRootGameObjects(cache);
    }

    private static void SetRootsActive(List<GameObject> roots, bool active)
    {
        for (int i = 0; i < roots.Count; i++)
        {
            if (roots[i]) roots[i].SetActive(active);
        }
    }
}
 