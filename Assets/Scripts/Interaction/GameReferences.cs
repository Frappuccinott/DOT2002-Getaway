using UnityEngine;

public class GameReferences : MonoBehaviour
{
    private static GameReferences _instance;
    public static GameReferences Instance 
    { 
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameReferences>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameReferences");
                    _instance = go.AddComponent<GameReferences>();
                }
            }
            return _instance;
        }
    }

    private PlayerInteraction _playerInteraction;
    public PlayerInteraction PlayerInteraction 
    { 
        get
        {
            if (_playerInteraction == null) _playerInteraction = FindFirstObjectByType<PlayerInteraction>();
            return _playerInteraction;
        }
    }

    private PlayerController _playerController;
    public PlayerController PlayerController 
    { 
        get
        {
            if (_playerController == null) _playerController = FindFirstObjectByType<PlayerController>();
            return _playerController;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
}
