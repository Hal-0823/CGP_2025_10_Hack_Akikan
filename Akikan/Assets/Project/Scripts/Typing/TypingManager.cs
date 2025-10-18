using UnityEngine;
using System.Collections.Generic; // List を使用するために必要
using System.Linq; // LINQ (ToList, Whereなど) を使用するために必要

/// <summary>
/// 複数レーンに対応したタイピングシステム全体のマネージャー。
/// キー入力を受け付け、アクティブなレーンに入力を振り分けます。
/// </summary>
public class TypingManager : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField, Tooltip("単語リストが記述されたTextAsset")]
    private TextAsset wordListAsset;

    [Header("Lane Configuration")]
    [SerializeField, Tooltip("シーン内の全WordLaneコンポーネント")]
    private List<WordLane> allLanes;

    // --- 内部状態 ---
    private List<string> wordBank; // wordListAssetから読み込んだ単語のリスト
    private List<WordLane> activeLanes; // 現在入力対象となっているレーンのリスト

    private void Start()
    {
        LoadWords();
        activeLanes = new List<WordLane>();
        InitializeLanes();
    }

    private void Update()
    {
        // 1. バックスペース処理
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            HandleBackspace();
            return; // このフレームでは他の入力は処理しない
        }

        // 2. 文字入力処理
        // Input.inputString を使うことで、Shiftキーなどを考慮した文字を取得
        if (Input.inputString.Length > 0)
        {
            char typedChar = Input.inputString[0];

            // 英字入力のみを処理 (小文字に統一)
            if (char.IsLetter(typedChar))
            {
                HandleLetterInput(char.ToLower(typedChar));
            }
        }
    }

    // --- Input Handling ---

    /// <summary>
    /// 文字入力のメインロジック。
    /// </summary>
    private void HandleLetterInput(char letter)
    {
        if (activeLanes.Count == 0)
        {
            // --- ケース1: レーン未選択状態 ---
            // 入力された文字が頭文字と一致するレーンをすべて探す
            List<WordLane> possibleLanes = allLanes.Where(lane =>
                !lane.IsWordComplete() && lane.GetNextLetter() == letter
            ).ToList();

            if (possibleLanes.Count > 0)
            {
                // 見つかったレーンをアクティブとして設定
                activeLanes = possibleLanes;
                // 見つかったすべてのレーンの入力を1文字進める
                foreach (var lane in activeLanes)
                {
                    lane.CheckAndAdvance(letter);
                }
            }
            // 一致するレーンがなければ、入力は無視される
        }
        else
        {
            // --- ケース2: レーン選択中 ---
            List<WordLane> remainingLanes = new List<WordLane>();
            bool wordCompleted = false;

            foreach (var lane in activeLanes)
            {  
                if (lane.CheckAndAdvance(letter))
                {
                    // 入力が正しく、さらに単語が完成した場合
                    if (lane.IsWordComplete())
                    {
                        OnWordCompleted(lane);
                        wordCompleted = true;
                    }
                    // 入力が正しいが、まだ単語が途中の場合
                    else
                    {
                        remainingLanes.Add(lane);
                    }
                }
                // 入力が間違っていた場合、このレーンは remainingLanes に追加されない
            }

            // 単語が1つでも完成した場合、選択状態をリセットする
            if (wordCompleted)
            {
                ResetAllLaneProgress();
            }
            else
            {
                // 入力ミスなどでアクティブ候補が0になった場合もリセット
                if (remainingLanes.Count == 0)
                {
                    ResetAllLaneProgress();
                }
                else
                {
                    // アクティブなレーンを更新し、非アクティブになったレーンをリセット
                    activeLanes = remainingLanes;
                    ResetInactiveLaneProgress();
                }
            }
        }
    }

    /// <summary>
    /// バックスペースキーが押された時の処理。
    /// </summary>
    private void HandleBackspace()
    {
        // レーン選択中（入力途中）であれば、選択状態を解除する
        if (activeLanes.Count > 0)
        {
            ResetAllLaneProgress();
        }
    }

    /// <summary>
    /// 単語が完成した時に呼ばれる処理。
    /// </summary>
    private void OnWordCompleted(WordLane completedLane)
    {
        Debug.Log($"Lane {completedLane.name} completed!");
        
        // ★ ここで缶を潰すなどの演出をトリガーする
        // (例: completedLane.GetComponent<Can>().Crush();)

        // レーンに次の単語をセットするよう命令
        completedLane.SetNextWord();
    }

    // --- Lane State Management ---

    /// <summary>
    /// 全てのレーンの入力進捗をリセットし、アクティブレーンをクリアします。
    /// </summary>
    private void ResetAllLaneProgress()
    {
        foreach (var lane in allLanes)
        {
            lane.ResetProgress();
        }
        activeLanes.Clear();
    }

    /// <summary>
    /// 現在アクティブなレーン "以外" の入力進捗をリセットします。
    /// </summary>
    private void ResetInactiveLaneProgress()
    {
        foreach (var lane in allLanes)
        {
            if (!activeLanes.Contains(lane))
            {
                lane.ResetProgress();
            }
        }
    }

    // --- Initialization & Word Supply ---

    /// <summary>
    /// TextAssetから単語リストを読み込み、wordBankに格納します。
    /// </summary>
    private void LoadWords()
    {
        if (wordListAsset == null)
        {
            Debug.LogError("Word List Assetが設定されていません！");
            wordBank = new List<string> { "error" }; // フォールバック
            return;
        }

        // テキストを改行文字で分割し、空の行は削除
        string[] words = wordListAsset.text.Split(
            new[] { '\n', '\r' },
            System.StringSplitOptions.RemoveEmptyEntries
        );
        wordBank = new List<string>(words);
    }

    /// <summary>
    /// ゲーム開始時に、各レーンに最初の単語を1つセットします。
    /// </summary>
    private void InitializeLanes()
    {
        foreach (var lane in allLanes)
        {
            // 最初の単語はSetWordで直接セットする
            lane.SetWord(GetRandomWord());
        }
    }

    /// <summary>
    /// 単語バンクからランダムな単語を1つ取得します。
    /// </summary>
    /// <returns>ランダムな単語</returns>
    public string GetRandomWord()
    {
        if (wordBank == null || wordBank.Count == 0)
        {
            Debug.LogError("Word Bankが空です。");
            return "empty";
        }
        return wordBank[Random.Range(0, wordBank.Count)];
    }

    /// <summary>
    /// （外部API）指定したレーンのキューに、指定した単語を追加します。
    /// </summary>
    /// <param name="laneIndex">レーンのインデックス (allLanesリストの順)</param>
    /// <param name="word">追加する単語</param>
    public void AddWordToSpecificLane(int laneIndex, string word)
    {
        if (laneIndex >= 0 && laneIndex < allLanes.Count)
        {
            allLanes[laneIndex].AddWordToQueue(word);
        }
        else
        {
            Debug.LogWarning($"不正なレーンIndexです: {laneIndex}");
        }
    }
}