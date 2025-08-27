using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ObstacleMovementSystem : MonoBehaviour
{
    [Header("Obstacle Hareket Ayarları")]
    [SerializeField] private float obstacleMoveSpeed = 10f;
    [SerializeField] private float despawnZ = -7f;

    [Header("Ground Hareket Ayarları")]
    [SerializeField] private float groundMoveSpeed = 8f;
    [SerializeField] private float groundDespawnZ = -10f;

    [Header("Ayrım")]
    [SerializeField] private string groundUnityTag = "Ground";

    [Header("Zorluk")]
    [SerializeField] private DifficultySystem difficulty; 

    [Header("Mobil Optimizasyon")]
    [SerializeField, Min(0)] private int initialObstacleCapacity = 64;
    [SerializeField, Min(0)] private int initialGroundCapacity = 8;

    private readonly List<Transform> _activeObstacles = new List<Transform>(4);
    private readonly List<Transform> _activeGrounds = new List<Transform>(4);
    private readonly HashSet<Transform> _obstacleSet = new HashSet<Transform>();
    private readonly HashSet<Transform> _groundSet = new HashSet<Transform>();

    void Awake()
    {
        if (difficulty == null) difficulty = FindObjectOfType<DifficultySystem>();

        // Önceden kapasite ver (GC önle)
        if (_activeObstacles.Capacity < initialObstacleCapacity)
            _activeObstacles.Capacity = initialObstacleCapacity;
        if (_activeGrounds.Capacity < initialGroundCapacity)
            _activeGrounds.Capacity = initialGroundCapacity;
    }

    void OnEnable()
    {
        ObjectPooler.OnSpawned += OnSpawned;
        GameLoop.OnTick += HandleTick;
    }

    void OnDisable()
    {
        ObjectPooler.OnSpawned -= OnSpawned;
        GameLoop.OnTick -= HandleTick;
    }

    private void OnSpawned(GameObject go)
    {
        if (!go) return;
        var t = go.transform;

        // HashSet ile kopya eklemeyi O(1)’de engelle
        if (go.CompareTag(groundUnityTag))
        {
            if (_groundSet.Add(t))
                _activeGrounds.Add(t);
        }
        else
        {
            if (_obstacleSet.Add(t))
                _activeObstacles.Add(t);
        }
    }

    private void HandleTick(float dt)
    {
        // Zorluktan gelen çarpanlar tek kez hesaplanır (cache)
        float obsSpeed = obstacleMoveSpeed * (difficulty ? difficulty.ObstacleSpeedMultiplier : 1f);
        float grdSpeed = groundMoveSpeed * (difficulty ? difficulty.GroundSpeedMultiplier : 1f);

        // Obstacles
        for (int i = _activeObstacles.Count - 1; i >= 0; i--)
        {
            var tr = _activeObstacles[i];
            // inaktif/NULL seyrek; OnDespawned zaten çıkarıyor, yine de koruyalım
            if (!tr || !tr.gameObject.activeInHierarchy)
            {
                _obstacleSet.Remove(tr);
                int last = _activeObstacles.Count - 1;
                _activeObstacles[i] = _activeObstacles[last];
                _activeObstacles.RemoveAt(last);
                continue;
            }

            Vector3 p = tr.position;
            p.z -= obsSpeed * dt;
            tr.position = p;

            if (p.z <= despawnZ)
            {
                _obstacleSet.Remove(tr);
                int last = _activeObstacles.Count - 1;
                _activeObstacles[i] = _activeObstacles[last];
                _activeObstacles.RemoveAt(last);
                ObjectPooler.Instance.ReturnToPool(tr.gameObject);
            }
        }

        // Grounds
        for (int i = _activeGrounds.Count - 1; i >= 0; i--)
        {
            var tr = _activeGrounds[i];
            if (!tr || !tr.gameObject.activeInHierarchy)
            {
                _groundSet.Remove(tr);
                int last = _activeGrounds.Count - 1;
                _activeGrounds[i] = _activeGrounds[last];
                _activeGrounds.RemoveAt(last);
                continue;
            }

            Vector3 p = tr.position;
            p.z -= grdSpeed * dt;
            tr.position = p;

            if (p.z <= groundDespawnZ)
            {
                _groundSet.Remove(tr);
                int last = _activeGrounds.Count - 1;
                _activeGrounds[i] = _activeGrounds[last];
                _activeGrounds.RemoveAt(last);
                ObjectPooler.Instance.ReturnToPool(tr.gameObject);
            }
        }
    }
}
