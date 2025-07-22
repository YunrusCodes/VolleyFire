using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Text;
using System.Collections.Generic;

public class Title : MonoBehaviour
{
    [Header("要加載的場景名稱")]
    public string sceneToLoad = "SampleScene";

    [Header("打字機效果設定")]
    public Text uiText;                   // Unity 的 UI.Text 元件
    [TextArea] public string fullText;   // 要顯示的完整句子
    public float typingSpeed = 0.05f;     // 每個字母出現的時間間隔

    // 供Animator事件呼叫：開始打字機效果
    public void PlayTypewriter()
    {
        StopAllCoroutines();
        StartCoroutine(TypeTextRich(fullText));
    }

    IEnumerator TypeTextRich(string text)
    {
        uiText.text = "";
        var visible = new StringBuilder(); // 最終要顯示的文字
        int i = 0;

        while (i < text.Length)
        {
            if (text[i] == '<')
            {
                // 尋找整個 tag
                int tagEnd = text.IndexOf('>', i);
                if (tagEnd != -1)
                {
                    string openingTag = text.Substring(i, tagEnd - i + 1);

                    // 嘗試尋找對應的關閉標籤
                    string tagName = openingTag.Trim('<', '>', '/').Split('=')[0];
                    string closingTag = $"</{tagName}>";

                    int closingIndex = text.IndexOf(closingTag, tagEnd + 1);
                    if (closingIndex != -1)
                    {
                        // 把標籤兩端取出
                        string innerText = text.Substring(tagEnd + 1, closingIndex - tagEnd - 1);

                        // 一次一個字加進去有標籤的區段中
                        for (int j = 0; j < innerText.Length; j++)
                        {
                            string partial = $"{openingTag}{innerText.Substring(0, j + 1)}{closingTag}";
                            uiText.text = visible.ToString() + partial;
                            yield return new WaitForSeconds(typingSpeed);
                        }

                        // 把整個段落加到 visible 內容中
                        visible.Append($"{openingTag}{innerText}{closingTag}");

                        // 跳到標籤之後
                        i = closingIndex + closingTag.Length;
                        continue;
                    }
                }
            }

            // 一般文字逐字顯示
            visible.Append(text[i]);
            uiText.text = visible.ToString();
            i++;
            yield return new WaitForSeconds(typingSpeed);
        }

        uiText.text = visible.ToString();
    }




    // 供Animator事件呼叫：進入場景
    public void GameStart()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
} 