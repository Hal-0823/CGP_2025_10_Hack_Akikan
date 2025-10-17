using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;
using System.Linq;

public class TypingManager : MonoBehaviour
{
    // 外部からの参照を簡易にする
    public static TypingManager I;

    // イベント
    public event Action OnTypeMiss;
    public event Action OnTypeCorrect;
    public event Action<string> OnTypeCorrectWithKey;
    public event Action OnNextQuestion;
    public event Action OnCompleteQuestion;

    // 次の問題出題までの遅延フレーム
    private const int _delayFrame = 20;
    [SerializeField] GameObject typingObj;
    [SerializeField] TextMeshProUGUI fText;
    [SerializeField] TextMeshProUGUI qText;
    [SerializeField] TextMeshProUGUI aText;
    [SerializeField] RectTransform textPanelRect;


    // 柔軟な入力を実装するための辞書
    private readonly ChangeDictionary _cd = new ChangeDictionary();

    // テキストアセット
    private TextAsset _furigana;
    private TextAsset _question;
    private TextAsset _textfile;

    // テキストデータを格納する
    private List<string> _fList;
    private List<string> _qList;
    private List<string> _fReadList = new List<string>();
    private List<string> _qReadList = new List<string>();

    // テキストの文字列
    private string _fString;
    private string _qString;
    private string _aString;

    // 何文字目を入力しているか
    private int _aNum;

    // タイプの正誤
    private bool _isCorrect;
    private bool _isTypingActive;

    private List<string> _romSliceList = new List<string>();
    private List<int> _furiCountList = new List<int>();
    private List<int> _romNumList = new List<int>();

    void Awake()
    {
        if (I == null)
        {
            I = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Start()
    {
        typingObj.SetActive(false);
    }

    void Update()
    {
        if (_isTypingActive) Typing();
    }

    public void SetTextAsset(TextAsset data)
    {
        _textfile = data;
        ReadText();
        SetList();
    }

    public void ActivateTyping()
    {
        _isTypingActive = true;
        typingObj.SetActive(true);
        OutPut();
    }

    public void DeactivateTyping()
    {
        _isTypingActive = false;
        typingObj.SetActive(false);
    }

    void Typing()
    {
        if (Input.anyKeyDown && _aNum<_aString.Length)
        {
            int furiCount = _furiCountList[_aNum];
            _isCorrect = false;

            //AudioManager.I.PlaySE(SE.Name.InputAttack);

            if (Input.GetKeyDown(_aString[_aNum].ToString()))
            {
                _isCorrect = true;
                Correct();
            }
            else if (Input.GetKeyDown("n") && furiCount>0 && _romSliceList[furiCount-1]=="n")
            {
                _romSliceList[furiCount-1] = "nn";
                _aString = string.Join("", GetRomSliceListWithoutSkip());
                ReCreateRomSliceList(_romSliceList);

                _isCorrect = true;
                Correct();
            }
            else
            {
                string currentFuri = _fString[furiCount].ToString();

                if (furiCount < _fString.Length-1)
                {
                    string addNextMoji = _fString[furiCount].ToString() + _fString[furiCount + 1].ToString();
                    CheckIrregularType(addNextMoji,furiCount,false);
                }

                if (!_isCorrect)
                {
                    string moji = _fString[furiCount].ToString();
                    CheckIrregularType(moji, furiCount, true);
                }
            }

            if (!_isCorrect)
            {
                Miss();
            }
        }
    }

    void CheckIrregularType(string currentFuri, int furiCount, bool addSmallMoji)
    {
        if (_cd.dic.ContainsKey(currentFuri))
        {
            List<string> stringList = _cd.dic[currentFuri];

            for (int i=0; i<stringList.Count; i++)
            {
                string rom = stringList[i];
                int romNum = _romNumList[_aNum];

                bool preCheck = true;

                for (int j=0; j<romNum; j++)
                {
                    if (rom[j] != _romSliceList[furiCount][j])
                    {
                        preCheck = false;
                    }
                }

                if (preCheck && Input.GetKeyDown(rom[romNum].ToString()))
                {
                    _romSliceList[furiCount] = rom;
                    _aString = string.Join("", GetRomSliceListWithoutSkip());

                    ReCreateRomSliceList(_romSliceList);

                    _isCorrect = true;

                    if (addSmallMoji) AddSmallMoji();

                    Correct();

                    break;
                }
            }
        }
    }

    async void Correct()
    {
        var token = this.GetCancellationTokenOnDestroy();

        // イベントを発生させる
        OnTypeCorrect?.Invoke();
        OnTypeCorrectWithKey?.Invoke(_aString[_aNum].ToString());

        _aNum++;
        // 表示する文字の色分け
        //char[] boostedKeys = { 'a', 'o', 'i' };  // 強化されたキー

        string coloredText = "<color=#323232>" + _aString.Substring(0,_aNum) + "</color>";
        coloredText += _aString.Substring(_aNum);
        /*foreach (char c in _aString.Substring(_aNum))
        {
            if (boostedKeys.Contains(c))
            {
                coloredText += $"<gradient=Enchant>{c}</gradient>"; // 赤色に変更
            }
            else
            {
                coloredText += c;
            }
        }*/

        aText.text = coloredText;
        if (_aNum >= _aString.Length)
        {
            // _delayFrame待つ
            await UniTask.DelayFrame(_delayFrame, PlayerLoopTiming.Update, cancellationToken: token); 
            OnCompleteQuestion?.Invoke();
            OutPut();
        }
    }

    void Miss()
    {
        OnTypeMiss?.Invoke();
    }

    public void OutPut()
    {
        OnNextQuestion?.Invoke();

        Debug.Log("次の問題！");
        if (_fList.Count < 1)
        {
            SetList();
        }

        // 0番目の文字に戻す
        _aNum = 0;

        // 末尾の問題番号
        int qNum = _fList.Count-1;

        _fString = _fList[qNum];
        _qString = _qList[qNum];

        // リストから末尾の問題を削除
        _fList.RemoveAt(qNum);
        _qList.RemoveAt(qNum);

        CreateRomSliceList(_fString);

        _aString = string.Join("", GetRomSliceListWithoutSkip());

        // テキストを変更する
        fText.text = _fString;
        qText.text = _qString;
        aText.text = _aString;

        // TextPanelの大きさをテキストの大きさに合わせる
        FitTextPanelSize();
    }

    void FitTextPanelSize()
    {
        //  それぞれのテキストサイズを取得
        float fWidth = fText.preferredWidth;
        float qWidth = qText.preferredWidth;
        float aWidth = aText.preferredWidth;

        //  テキストパネルの大きさを最大値に合わせる(1600を超える場合は1600になる(その場合はFontSizeが変動する))
        // textPanelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Math.Min(Mathf.Max(fWidth,qWidth,aWidth,100)+30, 780));
        textPanelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Math.Min(Mathf.Max(fWidth,qWidth,aWidth,200)+60, 1600));
    }

    void ReadText(bool random = true)
    {
        //string[] _fArray = _furigana.text.Split(new char[]{'\r','\n'},System.StringSplitOptions.RemoveEmptyEntries);
        //string[] _qArray = _question.text.Split(new char[]{'\r','\n'},System.StringSplitOptions.RemoveEmptyEntries);
        
        StringReader reader = new StringReader(_textfile.text);
        
        while (reader.Peek() != -1)
        {
            string row = reader.ReadLine();
            var columns = row.Split(',');

            _fReadList.Add(columns[1]);
            _qReadList.Add(columns[0]);
        }

        if (random)
        {
            int j = 0;
            for (int i = _fReadList.Count - 1; i >= 0; i--)
            {
                j = UnityEngine.Random.Range(0, _fReadList.Count);
                (_fReadList[i], _fReadList[j]) = (_fReadList[j], _fReadList[i]);
                (_qReadList[i], _qReadList[j]) = (_qReadList[j], _qReadList[i]);
            }
        }

        // リストは後ろから選び、削除していくので逆順にしておく
        _fReadList.Reverse();
        _qReadList.Reverse();
    }

    void SetList()
    {
        _fList = new List<string>(_fReadList);
        _qList = new List<string>(_qReadList);
    }

    // 柔軟な入力をしたとき、次の文字が小文字なら小文字を挿入する
    void AddSmallMoji()
    {
        int nextMojiNum = _furiCountList[_aNum]+1;

        if (_fString.Length-1 < nextMojiNum)
        {
            return;
        }

        string nextMoji = _fString[nextMojiNum].ToString();
        string a= _cd.dic[nextMoji][0];

        if (a[0]!='x' && a[0]!='l')
        {
            return;
        }

        // romSliceListに挿入と表示の反映
        _romSliceList.Insert(nextMojiNum, a);

        // SKIPを削除する
        _romSliceList.RemoveAt(nextMojiNum+1);

        // 変更したリストを再度表示させる
        ReCreateRomSliceList(_romSliceList);
        _aString = string.Join("", GetRomSliceListWithoutSkip());
    }

    void CreateRomSliceList(string moji)
    {
        _romSliceList.Clear();
        _furiCountList.Clear();
        _romNumList.Clear();

        for (int i=0; i<moji.Length; i++)
        {
            string a = _cd.dic[moji[i].ToString()][0];

            if (moji[i].ToString()=="ゃ" || moji[i].ToString()=="ゅ" || moji[i].ToString()=="ょ" || 
                moji[i].ToString()=="ぁ" || moji[i].ToString()=="ぃ" || moji[i].ToString()=="ぅ" || moji[i].ToString()=="ぇ" || moji[i].ToString()=="ぉ")
            {
                a = "SKIP";
            }
            else if (moji[i].ToString()=="っ" && i+1<moji.Length)
            {
                a = _cd.dic[moji[i+1].ToString()][0][0].ToString();
            }
            else if (i+1 < moji.Length)
            {
                string addNextMoji = moji[i].ToString() + moji[i+1].ToString();
                if (_cd.dic.ContainsKey(addNextMoji))
                {
                    a = _cd.dic[addNextMoji][0];
                }
            }

            _romSliceList.Add(a);

            if (a == "SKIP")
            {
                continue;
            }

            for (int j=0; j<a.Length; j++)
            {
                _furiCountList.Add(i);
                _romNumList.Add(j);
            }
        }
    }

    void ReCreateRomSliceList(List<string> romList)
    {
        _furiCountList.Clear();
        _romNumList.Clear();

        for (int i=0; i<romList.Count; i++)
        {
            string a = romList[i];

            if (a == "SKIP")
            {
                continue;
            }

            for (int j=0; j<a.Length; j++)
            {
                _furiCountList.Add(i);
                _romNumList.Add(j);
            }
        }
    }

    List<string> GetRomSliceListWithoutSkip()
    {
        List<string> returnList = new List<string>();
        foreach (string rom in _romSliceList)
        {
            if (rom == "SKIP")
            {
                continue;
            }
            returnList.Add(rom);
        }
        return returnList;
    }
}