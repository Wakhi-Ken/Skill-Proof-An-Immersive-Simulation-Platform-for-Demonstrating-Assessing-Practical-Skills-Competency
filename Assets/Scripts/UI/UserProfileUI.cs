using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserProfileUI : MonoBehaviour
{
    [Header("Profile fields")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text idText;
    [SerializeField] private TMP_Text emailText;
    [SerializeField] private TMP_Text attemptCount;

    [SerializeField] private Slider safetySlider;
    [SerializeField] private Slider competencySlider;
    [SerializeField] private Slider proceduralSlider;

    [Header("Refresh")]
    [Tooltip("Optional. Assign the menu button here, or hook UserProfileUI.Refresh in the button's OnClick instead.")]
    [SerializeField] private Button refreshButton;

    [Tooltip("Optional. Shows 'Loading...' or an error message.")]
    [SerializeField] private TMP_Text statusText;

    [Tooltip("Also load automatically when this canvas is switched on.")]
    [SerializeField] private bool loadOnEnable = true;

    [Tooltip("How long to wait for Firebase, the database manager and the logged-in user")]
    [SerializeField] private float maxWaitSeconds = 5f;

    [Tooltip("How many times to look for the profile document if it is not there yet")]
    [SerializeField] private int maxAttempts = 3;

    private bool isLoading;

    private void Awake()
    {
        if (refreshButton != null)
            refreshButton.onClick.AddListener(Refresh);
    }

    private void OnDestroy()
    {
        if (refreshButton != null)
            refreshButton.onClick.RemoveListener(Refresh);
    }

    private void OnEnable()
    {
        if (loadOnEnable) Refresh();
    }
    // refreshes the profile data. Hook this to a button's OnClick, or call it from another script.
    public async void Refresh()
    {
        await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        if (isLoading) return;          // ignore presses while a load is already running
        isLoading = true;
        SetButtonInteractable(false);
        SetStatus("Loading...");

        try
        {
            // Wait for Firebase, the database manager and the logged-in user
            float waited = 0f;
            while (!ServicesReady() && waited < maxWaitSeconds)
            {
                await Task.Yield();
                waited += Time.unscaledDeltaTime;
                if (this == null) return;
            }

            if (DatabaseManager.Instance == null)
            {
                SetStatus("Database not ready.");
                Debug.LogError("UserProfileUI: DatabaseManager was not found. Start from the first scene, "
                               + "or check that it is a root object in the scene.");
                return;
            }

            if (!FirebaseManager.IsReady || FirebaseAuth.DefaultInstance.CurrentUser == null)
            {
                SetStatus("Not logged in.");
                Debug.LogWarning("UserProfileUI: no logged-in user.");
                return;
            }

            string userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

            // The profile document can lag a moment behind a new login, so try a few times
            UserData user = null;
            for (int attempt = 0; attempt < Mathf.Max(1, maxAttempts); attempt++)
            {
                user = await DatabaseManager.Instance.GetUserAsync(userId);
                if (this == null) return;
                if (user != null) break;
                await Task.Delay(500);
                if (this == null) return;
            }

            if (user == null)
            {
                SetStatus("Profile not found.");
                Debug.LogWarning("UserProfileUI: no profile document found for this user.");
                return;
            }

            Apply(user);
            SetStatus(string.Empty);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            SetStatus("Could not load profile.");
        }
        finally
        {
            isLoading = false;
            SetButtonInteractable(true);
        }
    }

    private bool ServicesReady()
    {
        return FirebaseManager.IsReady
            && DatabaseManager.Instance != null
            && FirebaseAuth.DefaultInstance.CurrentUser != null;
    }

    private void Apply(UserData user)
    {
        SetText(nameText, user.name);
        SetText(idText, ShortId(user.userID));
        SetText(emailText, user.email);
        SetText(attemptCount, user.attemptCount.ToString());

        SetSlider(safetySlider, user.AverageSafety);
        SetSlider(competencySlider, user.AverageCompetency);
        SetSlider(proceduralSlider, user.AverageProcedural);
    }

    // Shortens long IDs for display: tCnYPIXqyXZiUcH4W8mLACVnZRg2 -> tCnY...ZRg2
    // IDs shorter than 12 characters are left unchanged. Display only: the full ID
    // is still stored in the database and in user.userID.
    private string ShortId(string id)
    {
        if (string.IsNullOrEmpty(id)) return id;
        return Regex.Replace(id, @"^(.{4}).{4,}(.{4})$", "$1...$2");
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target != null) target.text = value;
    }

    private void SetSlider(Slider slider, float value)
    {
        if (slider != null) slider.value = value;
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    private void SetButtonInteractable(bool value)
    {
        if (refreshButton != null) refreshButton.interactable = value;
    }
}