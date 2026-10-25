using System.Text;
using System.Threading.Tasks;
using Firebase.Auth;
using TMPro;
using UnityEngine;


public class DetailedGeneratedUI : MonoBehaviour
{
    [Header("Details canvas (leave inactive in the Hierarchy)")]
    [SerializeField] private GameObject detailedCanvas;

    [Header("User (all texts are optional)")]
    [SerializeField] private TMP_Text userNameText;
    [SerializeField] private TMP_Text userIdText;
    [SerializeField] private TMP_Text emailText;

    [Header("Simulation")]
    [SerializeField] private TMP_Text simulationNameText;
    [SerializeField] private TMP_Text activitiesSummaryText;   // e.g. 4/5
    [SerializeField] private TMP_Text activitiesText;          // one line per activity
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text completionStatusText;    // Completed or Incomplete

    [Header("Results")]
    [SerializeField] private TMP_Text competencyText;          // demonstrated or not (green / red)
    [SerializeField] private TMP_Text safetyText;
    [SerializeField] private TMP_Text errorsText;
    [SerializeField] private TMP_Text overallScoreText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Message while loading, or when there is nothing to show")]
    [SerializeField] private TMP_Text statusText;

    [Header("Colors")]
    [SerializeField] private Color goodColor = new Color(0.20f, 0.80f, 0.30f);   // green
    [SerializeField] private Color badColor = new Color(0.90f, 0.20f, 0.20f);    // red

    // The data currently on screen. The PDF certificate can reuse these.
    public PerformanceData CurrentPerformance { get; private set; }
    public UserData CurrentUser { get; private set; }

    private bool isLoading;

    private void Start()
    {
        if (detailedCanvas != null)
            detailedCanvas.SetActive(false);
    }

    // Opens the detailed results canvas and loads the data. Hook this to a button's OnClick.
    public async void Open()
    {
        if (isLoading) return;
        isLoading = true;

        if (detailedCanvas != null)
            detailedCanvas.SetActive(true);

        SetText(statusText, "Loading your results...");

        bool found = await LoadData();

        isLoading = false;
        if (this == null) return;

        if (!found)
        {
            SetText(statusText, "No completed simulation found yet.");
            return;
        }

        Fill(CurrentPerformance);
        SetText(statusText, "");
    }

    // back button
    public void Close()
    {
        if (detailedCanvas != null)
            detailedCanvas.SetActive(false);
    }

    // Loading

    private async Task<bool> LoadData()
    {
        FirebaseUser authUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (authUser == null || DatabaseManager.Instance == null)
            return false;

        CurrentUser = await DatabaseManager.Instance.GetUserAsync(authUser.UserId);

        PerformanceData p = null;

        // In the simulation scene the newest run is already in memory
        if (SessionTracker.Instance != null && SessionTracker.Instance.LastPerformance != null)
        {
            p = SessionTracker.Instance.LastPerformance;
        }
        else
        {
            // Otherwise take the newest saved run from the database
            var list = await DatabaseManager.Instance.GetPerformancesAsync(authUser.UserId);
            if (list.Count > 0) p = list[0];
        }

        CurrentPerformance = p;
        return p != null;
    }

    //Filling the screen

    private void Fill(PerformanceData p)
    {
        FirebaseUser authUser = FirebaseAuth.DefaultInstance.CurrentUser;

        // User details (falls back to the login details if the profile is missing)
        string name = (CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.name))
            ? CurrentUser.name
            : (authUser != null ? authUser.DisplayName : "");
        string id = CurrentUser != null ? CurrentUser.userID : (authUser != null ? authUser.UserId : "");
        string email = (CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.email))
            ? CurrentUser.email
            : (authUser != null ? authUser.Email : "");

        SetText(userNameText, name);
        SetText(userIdText, id);
        SetText(emailText, email);

        // Activities
        int done = 0;
        int total = 0;
        if (p.activities != null)
        {
            total = p.activities.Count;
            foreach (ActivityResult a in p.activities)
                if (a.completed) done++;
        }
        int notDone = total - done;

        SetText(simulationNameText, p.simulationName);
        SetText(activitiesSummaryText, done + "/" + total);
        SetText(activitiesText, BuildActivityList(p));

        // Date and time used
        SetText(dateText, p.completedAt.ToDateTime().ToLocalTime().ToString("dd MMM yyyy, HH:mm"));

        int seconds = Mathf.RoundToInt((float)p.completionTime);
        SetText(timeText, $"{seconds / 60:00}:{seconds % 60:00}");

        // Completion status
        if (completionStatusText != null)
        {
            completionStatusText.text = p.allTasksCompleted
                ? "Completed"
                : $"Incomplete ({notDone} not completed)";
            completionStatusText.color = p.allTasksCompleted ? goodColor : badColor;
        }

        // Competency: demonstrated or not
        if (competencyText != null)
        {
            competencyText.text = p.competent
                ? "Competency demonstrated"
                : "Competency not demonstrated";
            competencyText.color = p.competent ? goodColor : badColor;
        }

        float safety = (float)p.safetyScore;
        float overall = (safety + (float)p.competencyScore
                         + (float)p.proceduralScore + (float)p.compliance) / 4f;

        SetText(safetyText, Mathf.RoundToInt(safety) + "%");
        SetText(errorsText, p.errors.ToString());
        SetText(overallScoreText, Mathf.RoundToInt(overall) + "%");
        SetText(feedbackText, p.feedbackText);
    }

    private string BuildActivityList(PerformanceData p)
    {
        if (p.activities == null || p.activities.Count == 0)
            return "No activities recorded";

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < p.activities.Count; i++)
        {
            ActivityResult a = p.activities[i];

            string status = a.completed
                ? Colored("Completed", goodColor)
                : Colored("Not completed", badColor);

            string errorPart = a.errors > 0
                ? $" ({a.errors} error{(a.errors == 1 ? "" : "s")})"
                : "";

            sb.AppendLine($"{i + 1}. {a.name} - {status}{errorPart}");
        }

        return sb.ToString().TrimEnd();
    }

    // Helpers 

    private string Colored(string text, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target != null) target.text = value;
    }
}