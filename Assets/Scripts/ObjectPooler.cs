using System;
using System.Collections;
using UnityEngine;

public enum PoolTag : byte
{
    Ground = 0,
    Obstacle1 = 1,
    Obstacle2 = 2,
    Obstacle3 = 3,
    Obstacle4 = 4,
    Obstacle5 = 5,
    
    // İhtiyacınıza göre ekleyin…
}

[Serializable]
public struct PoolConfig
{
    public PoolTag tag;    // Inspector’dan seçilen enum
    public GameObject prefab; // Havuzlanacak prefab
    [Min(1)]
    public int size;   // Havuz boyutu
}

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance { get; private set; }
    public static event Action<GameObject> OnSpawned; // Her spawn’ta yayınlanacak

    [Header("Pool Ayarları")]
    [Tooltip("Her bir PoolTag için prefab ve boyut")]
    [SerializeField] private PoolConfig[] _configs;

    // Internal
    private GameObject[][] _instances;    // [configIndex][i]
    private Transform[][] _transforms;   // cache’lenmiş Transform’lar
    private int[] _nextIndex;    // çembersel sayaç
    private int[] _configIndex;  // PoolTag → configs indeksi haritası

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); return; }

        // Hazırlık
        int N = _configs.Length;
        _instances = new GameObject[N][];
        _transforms = new Transform[N][];
        _nextIndex = new int[N];
        _configIndex = new int[Enum.GetValues(typeof(PoolTag)).Length];

        // Başlangıçta hepsi -1
        for (int i = 0; i < _configIndex.Length; i++)
            _configIndex[i] = -1;
        // PoolTag → config indeksi
        for (int i = 0; i < N; i++)
            _configIndex[(int)_configs[i].tag] = i;

        // Frame‐frame yavaş instantiate için coroutine
        StartCoroutine(Prewarm());
    }

    private IEnumerator Prewarm()
    {
        for (int ci = 0; ci < _configs.Length; ci++)
        {
            var cfg = _configs[ci];
            int sz = cfg.size;
            _instances[ci] = new GameObject[sz];
            _transforms[ci] = new Transform[sz];

            for (int j = 0; j < sz; j++)
            {
                var obj = Instantiate(cfg.prefab, transform);
                obj.SetActive(false);
                _instances[ci][j] = obj;
                _transforms[ci][j] = obj.transform;
                yield return null; // her kare bir tane
            }
        }
    }

    /// <summary> Havuzdan çekip aktif eder ve spawn event’i yayınlar. </summary>
    public GameObject Spawn(PoolTag tag, Vector3 pos, Quaternion rot)
    {
        int ci = _configIndex[(int)tag];
        if (ci < 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[ObjectPooler] Tag {tag} bulunamadı!");
#endif
            return null;
        }

        var arrT = _transforms[ci];
        var pool = _instances[ci];
        int idx = _nextIndex[ci];

        // Pozisyon, rotasyon, aktif et
        arrT[idx].SetPositionAndRotation(pos, rot);
        pool[idx].SetActive(true);

        // Event
        OnSpawned?.Invoke(pool[idx]);

        // Sayaç ilerlet, çembersel
        idx++;
        if (idx >= pool.Length) idx = 0;
        _nextIndex[ci] = idx;

        return pool[idx];
    }

    /// <summary> Obje pasifleştirilip havuza döner. </summary>
    public void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
    }

    /// <summary> Dış dünyaya havuz konfigürasyonunu açıyoruz. </summary>
    public PoolConfig[] Configs => _configs;

    public void ReturnAllToPool()
    {
        for (int ci = 0; ci < _instances.Length; ci++)
        {
            var pool = _instances[ci];
            if (pool == null) continue;

            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] && pool[i].activeSelf)
                    pool[i].SetActive(false);
            }
        }
    }
}
