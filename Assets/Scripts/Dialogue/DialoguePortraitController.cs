using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

/// <summary>
/// 對話頭像控制器：支援 Yarn Spinner 3.x 指令 <<setPortrait "角色名">>
/// </summary>
public class DialoguePortraitController : MonoBehaviour
{
    [Header("頭像 Image 物件")]
    public Image portraitImage;

    [Header("頭像圖庫")]
    public Sprite defaultPortrait;
    public Sprite aliciaPortrait;
    // TODO: 你可以在 Inspector 加更多角色頭像

    [Header("Yarn DialogueRunner (可於 Inspector 指定)")]
    public DialogueRunner dialogueRunner;

    private void Awake()
    {
        // 優先使用 Inspector 指定的 dialogueRunner，否則自動尋找
        var runner = dialogueRunner != null ? dialogueRunner : FindObjectOfType<DialogueRunner>();
        if (runner != null)
        {
            runner.AddCommandHandler<string>("setPortrait", SetPortrait);
        }
    }

    /// <summary>
    /// Yarn 指令：<<setPortrait "Alicia">>
    /// </summary>
    /// <param name="portraitName">角色名</param>
    public void SetPortrait(string portraitName)
    {
        switch (portraitName)
        {
            case "Alicia":
                portraitImage.sprite = aliciaPortrait;
                break;
            // TODO: 你可以加更多角色
            default:
                portraitImage.sprite = defaultPortrait;
                break;
        }
    }
} 