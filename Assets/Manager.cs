using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Functions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Manager : MonoBehaviour
{
    private FirebaseFunctions _functions;
    private FirebaseDatabase _reference;
    [SerializeField] private Transform _mContentContainer;
    [SerializeField] private GameObject _mItemPrefab;
    [SerializeField] private ScrollRect _scrollView;
    [SerializeField] private Button _searchButton;
    [SerializeField] private TMP_InputField _searchInput;

    private void Awake()
    {
        _scrollView.gameObject.SetActive(false);
        _functions = FirebaseFunctions.DefaultInstance;
        Debug.Log("start");
    }

    // Start is called before the first frame update
    private void Start()
    {
        _searchButton.GetComponent<Button>().onClick.AddListener(TaskSearchSong);
        _reference = FirebaseDatabase.DefaultInstance;
        fetchSongNamesAndIds();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)) TaskSearchSong();
    }

    private void SearchForSong(string songName)
    {
        Debug.Log(songName);
        GetSimilarSongs("0", 5, 0.5)
            .ContinueWith(data =>
            {
                foreach (var key in data.Result.Keys) Debug.Log(key);
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
        return function.CallAsync(data).ContinueWith(task => task.Result.Data as IDictionary);
    }

    private void fetchSongNamesAndIds()
    {
        _reference.GetReference("songs").OrderByChild("title")
            .GetValueAsync().ContinueWithOnMainThread(task => {
                if (task.IsFaulted) {
                    // Handle the error...
                }
                else if (task.IsCompleted) {
                    Debug.Log("got the names");
                    DataSnapshot snapshot = task.Result;
                    // Do something with snapshot...
                }
            });
    }
}