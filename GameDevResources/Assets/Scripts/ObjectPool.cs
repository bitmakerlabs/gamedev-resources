using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ObjectPool : MonoBehaviour
{
    #region Classes, Structs & Enums
    [System.Serializable]
    public class PoolDefinition
    {
        public GameObject Prefab;
        public int Count;
    };

    private class GameObjectMeta
    {
        public GameObject Object;
        public System.DateTime TimeStamp;
        public GameObjectMeta()
        {
            TimeStamp = System.DateTime.Now;
        }
    };

    public enum PoolBehaviour
    {
        HardLimit,
        RecycleOnMax,
        GrowOnMax
    }
    #endregion

    #region Public Member Variables
    public PoolDefinition[] PoolDefinitions;
    public PoolBehaviour Behaviour = PoolBehaviour.HardLimit;
    #endregion

    #region Private Member Variables
    private static ObjectPool _Instance = null;

    private Dictionary<GameObjectMeta, GameObject> _InstantiatedObjects = new Dictionary<GameObjectMeta, GameObject>();
    private Dictionary<int, List<GameObject>> _PooledObjects = new Dictionary<int, List<GameObject>>();
    #endregion

    #region Public Properties
    public static ObjectPool Instance
    {
        get { return _Instance; }
    }
    #endregion

    #region Public Methods
    public static void CreatePool(GameObject prefab, int poolSize)
    {
        if (prefab != null)
        {
            if( !_Instance._PooledObjects.ContainsKey(prefab.GetInstanceID()) )
            {
                List<GameObject> gos = new List<GameObject>();
                _Instance._PooledObjects.Add(prefab.GetInstanceID(), gos);

                if( poolSize > 0 )
                {
                    bool active = prefab.activeSelf;
                    prefab.SetActive(false);

                    while (gos.Count < poolSize)
                    {
                        GameObject obj = (GameObject)Object.Instantiate(prefab);
                        obj.transform.parent = _Instance.transform;
                        gos.Add(obj);
                    }

                    prefab.SetActive(active);
                }                
            }
        }
    }

    public static void CreatePool<T>(T prefab, int initialPoolSize) where T : Component
    {
        CreatePool(prefab.gameObject, initialPoolSize);
    }

    public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
    {
        List<GameObject> gos;

        if (_Instance._PooledObjects.TryGetValue(prefab.GetInstanceID(), out gos))
        {
            GameObject obj = null;
            Transform trans = null;

            if (gos.Count > 0)
            {
                while (obj == null && gos.Count > 0)
                {
                    obj = gos[0];
                    gos.RemoveAt(0);
                }

                if (obj != null)
                {
                    trans = obj.transform;
                    trans.parent = parent;
                    trans.localPosition = position;
                    trans.localRotation = rotation;
                    obj.SetActive(true);

                    GameObjectMeta gom = new GameObjectMeta();
                    gom.Object = obj;

                    _Instance._InstantiatedObjects.Add(gom, prefab);

                    return obj;
                }
            }

            switch (_Instance.Behaviour)
            {
                case PoolBehaviour.HardLimit:
                    obj = null;
                    break;
                case PoolBehaviour.GrowOnMax:
                    obj = (GameObject)Object.Instantiate(prefab);
                    trans = obj.transform;
                    trans.parent = parent;
                    trans.localPosition = position;
                    trans.localRotation = rotation;
                    GameObjectMeta gom = new GameObjectMeta();
                    gom.Object = obj;
                    _Instance._InstantiatedObjects.Add(gom, prefab);                
                    break;
                case PoolBehaviour.RecycleOnMax:
                    List<KeyValuePair<GameObjectMeta,GameObject>> gosOfType = _Instance._InstantiatedObjects.Where( pair => pair.Value == prefab).ToList();
                    GameObjectMeta oldestGOM = null;                    

                    foreach( KeyValuePair<GameObjectMeta,GameObject> kvp in gosOfType)
                    {
                        if (oldestGOM == null)
                        {
                            oldestGOM = kvp.Key;
                        }
                        else
                        {
                            if( System.DateTime.Compare(kvp.Key.TimeStamp, oldestGOM.TimeStamp) < 0 )
                            {
                                oldestGOM = kvp.Key;
                            }
                        }
                    }

                    if (oldestGOM != null)
                    {
                        Recycle(oldestGOM, prefab);

                        // now that we KNOW one is free, recursively call spawn (only once though)
                        Spawn(prefab, parent, position, rotation);
                    }
                    break;
            }                

            return obj;
        }
        else
        {
            GameObject obj = (GameObject)Object.Instantiate(prefab);
            Transform trans = obj.GetComponent<Transform>();
            trans.parent = parent;
            trans.localPosition = position;
            trans.localRotation = rotation;
            return obj;
        }        
    }

    public static void Recycle(GameObject obj)
    {
        GameObject prefab = null;

        List<KeyValuePair<GameObjectMeta, GameObject>> kvps = _Instance._InstantiatedObjects.Where( o => o.Key.Object.GetInstanceID() == obj.GetInstanceID() ).ToList();

        if( kvps.Count > 0 )
        {
            prefab = kvps.First().Value;
            Recycle(kvps.First().Key, prefab);
        }
        else
        {
            Object.Destroy(obj);
        }
    }
    private static void Recycle(GameObjectMeta obj, GameObject prefab)
    {
        _Instance._PooledObjects[prefab.GetInstanceID()].Add(obj.Object);
        _Instance._InstantiatedObjects.Remove(obj);
        obj.Object.transform.parent = _Instance.transform;
        obj.Object.SetActive(false);
    }

    #endregion

    #region Unity Methods
    private void Awake()
    {
        _Instance = this;

        // instantiate the object pools
        for(int i=0; i < PoolDefinitions.Length; i++)
        {
            CreatePool(PoolDefinitions[i].Prefab, PoolDefinitions[i].Count);
        }
    }
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

    }
    #endregion
}
