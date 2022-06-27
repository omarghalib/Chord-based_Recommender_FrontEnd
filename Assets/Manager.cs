using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Functions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class Manager : MonoBehaviour
{
    [SerializeField] private ScrollRect _scrollView;
    [SerializeField] private TMP_InputField _searchInput;
    [SerializeField] private Button _searchButton;
    [SerializeField] private Transform _mContentContainer;
    [SerializeField] private GameObject _mItemPrefab;
    private FirebaseFunctions _functions;
    void Awake()
    {
        _scrollView.gameObject.SetActive(false);
        _functions = FirebaseFunctions.DefaultInstance;
        Debug.Log("start");
    }

    // Start is called before the first frame update
    void Start()
    {
        _searchButton.GetComponent<Button>().onClick.AddListener(TaskSearchSong);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            TaskSearchSong();
        }
    }

    private void SearchForSong(string songName)
    {
        Debug.Log(songName);
        GetSimilarSongs("0", 5, 0.5)
            .ContinueWith((data) =>
            {
                foreach (var key in data.Result.Keys)
                {
                    Debug.Log(key);
                }
            });
    }

    private void AddToSongsScrollArea(string songName)
    {
        _scrollView.gameObject.SetActive(true);
        var itemGo = Instantiate(_mItemPrefab);
        itemGo.GetComponent<TMP_Text>().text = songName;
        //parent the item to the content container
        itemGo.transform.SetParent(_mContentContainer);
        //reset the item's scale -- this can get munged with UI prefabs
        itemGo.transform.localScale = Vector2.one;
    }

    private void TaskSearchSong()
    {
        SearchForSong(_searchInput.text);
    }

    private Task<IDictionary> GetSimilarSongs(string songId, int numOfSongs, double minSimilarityScore)
    {
        var data = new Dictionary<string, object>
        {
            ["songId"] = songId,
            ["numOfSongs"] = numOfSongs,
            ["minSimilarityScore"] = minSimilarityScore
        };
        var function = _functions.GetHttpsCallable("getSongRecommendations");
        return function.CallAsync(data).ContinueWith((task) => task.Result.Data as IDictionary);    }
}