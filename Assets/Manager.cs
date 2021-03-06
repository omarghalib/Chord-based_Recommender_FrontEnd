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
    [SerializeField] private Scrollbar _similarityScrollbar;
    [SerializeField] private TextMeshProUGUI _similarityText;
    private Dictionary<string, string> _songNames = new();
    private Dictionary<string, string> _songIds = new();
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
        _similarityScrollbar.onValueChanged.AddListener(UpdateSimilarityText);
        _reference = FirebaseDatabase.DefaultInstance;
        FetchSongNamesAndIds().ContinueWith(snapshot =>
        {
            foreach (var child in snapshot.Result.Children)
            {
                string title = (string) child.Child("title").Value;
                if (!_songIds.ContainsKey(title))
                {
                    _songIds[title] = child.Key;
                }

                _songNames[child.Key] = title;
            }
        });
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)) TaskSearchSong();
    }

    private void SearchForSong(string songName)
    {
        double minSimilarityScore = _similarityScrollbar.value;
        GetSimilarSongs(_songIds[songName], 5, minSimilarityScore)
            .ContinueWithOnMainThread(data =>
            {
                ClearSongsScrollArea();
                foreach (string key in data.Result.Keys)
                {
                    Debug.Log(key);
                    Debug.Log(_songNames[key]);
                    AddToSongsScrollArea(_songNames[key]);
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

    private void ClearSongsScrollArea()
    {
        for (int i = 0; i < _mContentContainer.childCount; i++)
        {
            Destroy(_mContentContainer.GetChild(i).gameObject);
        }
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

    private Task<DataSnapshot> FetchSongNamesAndIds()
    {
        return _reference.GetReference("songs").OrderByChild("title")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) {
                    Debug.Log("error fetching names");
                }
                else if (task.IsCompleted) {
                    Debug.Log("got the names");
                    DataSnapshot snapshot = task.Result;
                    Debug.Log(snapshot.ChildrenCount);
                    return task.Result;
                }

                return null;
            });
    }

    private void UpdateSimilarityText(float val)
    {
        int newVal = (int) (val * 100);
        _similarityText.text = "Minimum similar chords: "+newVal+"%";
    }
}