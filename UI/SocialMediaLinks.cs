using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SocialMediaLinks : MonoBehaviour
{
    [SerializeField] private Button _discordButton;
    [SerializeField] private string _discordInviteURL = "";

    private void OnEnable()
    {
        if (_discordButton != null)
        {
            _discordButton.onClick.AddListener(OpenDiscord);
        }
    }

    private void OnDisable()
    {
        if (_discordButton != null)
        {
            _discordButton.onClick.RemoveListener(OpenDiscord);
        }
    }

    public void OpenDiscord()
    {
        //// This protocol tells the OS to look for the Discord App
        //string discordAppUrl = "discord://discord.gg/" + _discordInviteCode;

        //// This is the fallback web address
        //string discordWebUrl = "https://discord.gg/" + _discordInviteCode;

        OpenURLSafely(_discordInviteURL);
    }

    /// <summary>
    /// Opens a URL safely, ensuring the browser gets focus.
    /// Temporarily switches out of fullscreen modes if needed to allow proper focus transfer.
    /// </summary>
    private void OpenURLSafely(string url)
    {
        if (string.IsNullOrEmpty(url)) return;

        // Check current fullscreen mode
        FullScreenMode currentMode = Screen.fullScreenMode;
        bool wasFullscreen = currentMode == FullScreenMode.FullScreenWindow ||
                            currentMode == FullScreenMode.ExclusiveFullScreen;

        if (wasFullscreen)
        {
            // Switch to windowed mode to allow browser to properly take focus
            Screen.fullScreenMode = FullScreenMode.Windowed;

            // Open URL after a brief delay to ensure window mode switch completes
            StartCoroutine(OpenURLDelayed(url, restoreMode: currentMode));
        }
        else
        {
            // Already windowed - open URL directly
            #if !UNITY_EDITOR
            Application.runInBackground = true;
            #endif

            Application.OpenURL(url);
        }
    }

    private IEnumerator OpenURLDelayed(string url, FullScreenMode restoreMode)
    {
        // Wait for the window mode change to complete
        yield return new WaitForSeconds(0.1f);

        // Ensure app can run in background so browser can take focus
        Application.runInBackground = true;

        // Open the URL - should now properly focus the browser
        Application.OpenURL(url);

        // Wait before restoring fullscreen mode to ensure browser fully opened
        yield return new WaitForSeconds(1f);

        // Restore original fullscreen mode
        Screen.fullScreenMode = restoreMode;
    }
}
