using UnityEngine;
using TMPro; // TextMeshProUGUI を使用するために必要
using System.Collections.Generic; // Queue を使用するために必要

/// <summary>
/// タイピングゲームの各レーンを管理するコンポーネント。
/// 単語のキュー（待ち行列）と現在の入力進捗を保持します。
/// </summary>
public class WordLane : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField, Tooltip("単語を表示するためのUIテキスト")]
    private TextMeshProUGUI wordText;

    [Header("Display Settings")]
    [SerializeField, Tooltip("入力済み文字の色")]
    private Color typedColor = Color.green;

    [SerializeField, Tooltip("未入力文字の色")]
    private Color defaultColor = Color.white;

    // --- 内部状態 ---
    private string currentWord = ""; // 現在表示・入力対象の単語
    private int typedIndex = 0; // 現在の入力進捗インデックス
    private Queue<string> wordQueue = new Queue<string>(); // 単語の待ち行列

    private void Start()
    {
        // 初期状態（空白）でUIを更新
        UpdateDisplay();
    }

    // --- Public Methods (TypingManagerやSpawnerから呼ばれる) ---

    /// <summary>
    /// このレーンの待ち行列に新しい単語を追加します。
    /// </summary>
    /// <param name="newWord">追加する単語</param>
    public void AddWordToQueue(string newWord)
    {
        wordQueue.Enqueue(newWord.ToLower());

        // もしレーンが現在空白（待機状態）であれば、即座に次の単語を表示する
        if (string.IsNullOrEmpty(currentWord))
        {
            SetNextWord();
        }
    }

    /// <summary>
    /// 待ち行列から次の単語を取り出し、現在の単語として設定します。
    /// キューが空の場合、レーンは空白状態になります。
    /// </summary>
    public void SetNextWord()
    {
        if (wordQueue.Count > 0)
        {
            // キューから次の単語を取得
            currentWord = wordQueue.Dequeue();
        }
        else
        {
            // キューが空なら空白にする
            currentWord = "";
        }
        
        typedIndex = 0;
        UpdateDisplay();
    }

    /// <summary>
    /// （初期化用）単語を強制的にセットします。キューを無視します。
    /// </summary>
    public void SetWord(string newWord)
    {
        currentWord = newWord.ToLower();
        typedIndex = 0;
        UpdateDisplay();
    }

    /// <summary>
    /// 現在の単語の入力進捗をリセットします。
    /// </summary>
    public void ResetProgress()
    {
        typedIndex = 0;
        UpdateDisplay();
    }

    // --- Internal Methods (TypingManagerからの入力判定用) ---

    /// <summary>
    /// 次に入力すべき文字を返します。
    /// </summary>
    /// <returns>次の文字。単語の終端または空白の場合は '\0' を返す。</returns>
    public char GetNextLetter()
    {
        if (string.IsNullOrEmpty(currentWord) || typedIndex >= currentWord.Length)
        {
            return '\0'; // 単語がない、または入力完了
        }
        return currentWord[typedIndex];
    }

    /// <summary>
    /// 入力された文字が正しいかチェックし、正しければ進捗を進めます。
    /// </summary>
    /// <param name="letter">入力された文字</param>
    /// <returns>入力された文字が正しければ true</returns>
    public bool CheckAndAdvance(char letter)
    {
        if (GetNextLetter() == letter)
        {
            typedIndex++;
            UpdateDisplay();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 現在の単語が入力完了したかを返します。
    /// </summary>
    public bool IsWordComplete()
    {
        if (string.IsNullOrEmpty(currentWord))
        {
            return false;
        }
        return typedIndex >= currentWord.Length;
    }

    /// <summary>
    /// 現在の入力進捗インデックスを返します。
    /// </summary>
    public int GetCurrentIndex()
    {
        return typedIndex;
    }

    // --- Private Methods ---

    /// <summary>
    /// UIテキストの表示を、現在の入力進捗に合わせて更新します。
    /// (入力済みと未入力で色分け処理)
    /// </summary>
    private void UpdateDisplay()
    {
        if (wordText == null) return;

        // 単語がセットされていない（空白）の場合
        if (string.IsNullOrEmpty(currentWord))
        {
            wordText.text = "";
            return;
        }

        // TMProのリッチテキストを使用して色分け
        string typedPart = currentWord.Substring(0, typedIndex);
        string remainingPart = currentWord.Substring(typedIndex);

        wordText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(typedColor)}>{typedPart}</color><color=#{ColorUtility.ToHtmlStringRGB(defaultColor)}>{remainingPart}</color>";
    }
}