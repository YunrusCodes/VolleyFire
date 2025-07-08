using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

public class PlayerHealth : BaseHealth
{
    private DialogueRunner dialogueRunner;

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;
        gameObject.SetActive(false);

        dialogueRunner = FindObjectOfType<DialogueRunner>();
        if (dialogueRunner != null)
        {
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
            dialogueRunner.StartDialogue("MissionFailed");
            PlayerController.EnableFire(false);
        }
    }

    private void OnDialogueComplete()
    {
        // 解除註冊，避免重複
        dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
        // 重新載入場景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
} 