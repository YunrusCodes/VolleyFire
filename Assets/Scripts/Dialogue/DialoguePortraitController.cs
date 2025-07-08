using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class DialoguePortraitController : MonoBehaviour
{
    [Header("頭像 Image 物件")]
    public Image portraitImage;

    [Header("預設頭像")]
    public Sprite defaultPortrait;

    [Header("Yarn DialogueRunner (可於 Inspector 指定)")]
    public DialogueRunner dialogueRunner;

    private void Awake()
    {
        var runner = dialogueRunner != null ? dialogueRunner : FindObjectOfType<DialogueRunner>();
        if (runner != null)
        {
            runner.AddCommandHandler<string>("setPortrait", SetPortrait);
        }
    }

    /// <summary>
    /// Yarn 指令：<<setPortrait "角色名">>
    /// </summary>
    public void SetPortrait(string portraitName)
    {
        // 從 Resources/Portraits/ 依名字自動載入
        Sprite sprite = Resources.Load<Sprite>($"Portraits/{portraitName}");
        if (sprite != null)
        {
            portraitImage.sprite = sprite;
        }
        else
        {
            portraitImage.sprite = defaultPortrait;
        }
    }
} 