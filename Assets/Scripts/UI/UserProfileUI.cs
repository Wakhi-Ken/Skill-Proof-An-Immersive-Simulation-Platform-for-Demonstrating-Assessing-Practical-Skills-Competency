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

    [Header("Slider colours (scores are 0-100)")]
    [Tooltip("Below this score the slider uses the Low colour")]
    [SerializeField] private float lowBelow = 50f;
    [Tooltip("Below this score (and above Low) the slider uses the Medium colour. At or above it, High.")]
    [SerializeField] private float mediumBelow = 70f;
    [SerializeField] private Color lowColor = new Color(0.90f, 0.25f, 0.25f);     // red
    [SerializeField] private Color mediumColor = new Color(0.98f, 0.75f, 0.20f);  // amber
    [SerializeField] private Color highColor = new Color(0.20f, 0.75f, 0.35f);    // green

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
    private bool refreshQueued;
    private SessionTracker subscribedTracker;

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
        // Reload as soon as a finished session has been saved
        subscribedTracker = SessionTracker.Instance;
        if (subscribedTracker != null)
            subscribedTracker.OnPerformanceSaved += HandlePerformanceSaved;

        if (loadOnEnable) Refresh();
    }

    private void OnDisable()
    {
        if (subscribedTracker != null)
            subscribedTracker.OnPerformanceSaved -= HandlePerformanceSaved;
        subscribedTracker = null;
    }

    private void HandlePerformanceSaved(bool saved)
    {
        if (saved) Refresh();
    }

    // Refreshes the profile data. Hook this to a button's OnClick, or call it from another script.
    public async void Refresh()
    {
        await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        if (isLoading)
        {
            refreshQueued = true;   // run once more when the current load ends
            return;
        }

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

            if (refreshQueued)
            {
                refreshQueued = false;
                Refresh();
            }
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

    // score is 0-100. Works whatever the slider's min/max are.
    private void SetSlider(Slider slider, float score)
    {
        if (slider == null) return;

        float clamped = Mathf.Clamp(score, 0f, 100f);
        slider.value = Mathf.Lerp(slider.minValue, slider.maxValue, clamped / 100f);

        // Colour the filled part of the slider by score
        if (slider.fillRect != null)
        {
            Image fill = slider.fillRect.GetComponent<Image>();
            if (fill != null) fill.color = ColorForScore(clamped);
        }
    }

    private Color ColorForScore(float score)
    {
        if (score < lowBelow) return lowColor;
        if (score < mediumBelow) return mediumColor;
        return highColor;
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