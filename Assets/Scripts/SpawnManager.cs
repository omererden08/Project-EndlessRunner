using UnityEngine;

[DisallowMultipleComponent]
public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Noktası")]
    [SerializeField] private Transform _obstacleSpawnPoint;
    [SerializeField] private Transform _groundSpawnPoint;

    private static readonly Vector3 _firstSpawnPoint = new Vector3(0f, -3.3f, 25f);

    [Header("Zamanlama")]
    [SerializeField, Min(0f)] private float _groundSpawnInterval = 0.2f;
    [SerializeField, Min(0f)] private float _obstacleSpawnInterval = 2.4f; // easy

    [Header("Obstacle Ayarları")]
    [SerializeField] private PoolTag[] _obstacleTags;

    [Header("Zorluk")]
    [SerializeField] private DifficultySystem difficulty; // Inspector’dan bağla

    [Header("Mobil Optimizasyon")]
    [Tooltip("Düşük FPS’te tek frameda yapılacak maksimum obstacle spawn sayısı")]
    [SerializeField, Min(1)] private int _maxObstacleSpawnsPerFrame = 5;

    private float _groundTimer;
    private float _obstacleTimer;
    private int _tagCount;

    private int _lastRandomLaneX = int.MinValue;
    private int _lastObstacleIdx = -1;

    private bool _firstGroundSpawnDone = false;

    void Awake()
    {
        if (difficulty == null) difficulty = FindObjectOfType<DifficultySystem>();

        _tagCount = _obstacleTags?.Length ?? 0;
        if (_tagCount == 0)
        {
            enabled = false;
            return;
        }

        if (_obstacleTags[0] != PoolTag.Ground)
        {
            // İlk tag Ground olmalı (bilgi)
            // Debug.LogWarning($"[{name}] İlk tag Ground olmalı! Şu an: {_obstacleTags[0]}");
        }

        if (!_groundSpawnPoint) _groundSpawnPoint = transform;
        if (!_obstacleSpawnPoint) _obstacleSpawnPoint = transform;
    }

    void Start()
    {
        if (_tagCount > 0)
            SpawnGround();

        int idx;
        do
        {
            idx = Random.Range(1, _tagCount);
        } while (idx == _lastObstacleIdx && _tagCount > 2);

        _lastObstacleIdx = idx;
        Spawn(_obstacleTags[idx], idx);


    }

    void OnEnable()
    {
        ResetState(); 
        GameLoop.OnTick += HandleTick;
    }
    void OnDisable() => GameLoop.OnTick -= HandleTick;

    private void HandleTick(float dt)
    {
        if (_tagCount == 0) return;

        // --- Ground spawn ---
        if (_groundSpawnInterval > 0f)
        {
            _groundTimer += dt;
            while (_groundTimer >= _groundSpawnInterval)
            {
                _groundTimer -= _groundSpawnInterval;
                SpawnGround();
            }
        }

        // --- Obstacle spawn ---
        if (_tagCount > 1 && _obstacleSpawnInterval > 0f)
        {
            float intervalMul = difficulty ? difficulty.ObstacleSpawnIntervalMultiplier : 1f;
            float currentInterval = Mathf.Max(0.03f, _obstacleSpawnInterval * intervalMul);

            _obstacleTimer += dt;

            int spawnsThisFrame = 0;
            while (_obstacleTimer >= currentInterval && spawnsThisFrame < _maxObstacleSpawnsPerFrame)
            {
                _obstacleTimer -= currentInterval;
                spawnsThisFrame++;

                int idx;
                do
                {
                    idx = Random.Range(1, _tagCount);
                } while (idx == _lastObstacleIdx && _tagCount > 2);

                _lastObstacleIdx = idx;
                Spawn(_obstacleTags[idx], idx);

                // eğri hızla değişiyorsa döngü içinde güncelle
                if (difficulty)
                {
                    intervalMul = difficulty.ObstacleSpawnIntervalMultiplier;
                    currentInterval = Mathf.Max(0.03f, _obstacleSpawnInterval * intervalMul);
                }
            }
        }
    }

    private void SpawnGround()
    {
        var tag = _obstacleTags[0];
        Vector3 pos = !_firstGroundSpawnDone ? _firstSpawnPoint : _groundSpawnPoint.position;
        _firstGroundSpawnDone = true;

        ObjectPooler.Instance.Spawn(tag, pos, Quaternion.identity);
    }

    private void Spawn(PoolTag tag, int idx)
    {
        Vector3 pos = _obstacleSpawnPoint.position;

        // 1..3 indexleri şeritli engeller ise şerit x ata
        if (idx >= 1 && idx <= 3)
            pos.x = GetRandomLaneX();
        else
            pos.x = 0f;

        ObjectPooler.Instance.Spawn(tag, pos, Quaternion.identity);
    }

    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private int GetRandomLaneX()
    {
        int x;
        do { x = Random.Range(-1, 2); } while (x == _lastRandomLaneX);
        _lastRandomLaneX = x;
        return x;
    }

    public void ResetState()
    {
        _groundTimer = 0f;
        _obstacleTimer = 0f;
        _lastRandomLaneX = int.MinValue;
        _lastObstacleIdx = -1;
        _firstGroundSpawnDone = false;
    }

}
