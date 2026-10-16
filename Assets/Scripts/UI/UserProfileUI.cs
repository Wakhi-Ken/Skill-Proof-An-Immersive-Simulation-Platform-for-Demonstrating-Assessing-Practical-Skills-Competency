using System.Threading.Tasks;
using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserProfileUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text idText;
    [SerializeField] private TMP_Text emailText;
    [SerializeField] private TMP_Text attemptCount;

    [SerializeField] private Slider safetySlider;
    [SerializeField] private Slider competencySlider;
    [SerializeField] private Slider proceduralSlider;

    [Tooltip("How long to wait for Firebase and the database manager to be ready")]
    [SerializeField] private float maxWaitSeconds = 5f;

    private async void OnEnable()
    {
        // Wait until Firebase and DatabaseManager exist, instead of crashing if this
        // canvas is switched on a moment before they are ready
        float waited = 0f;
        while ((!FirebaseManager.IsReady || DatabaseManager.Instance == null) && waited < maxWaitSeconds)
        {
            await Task.Yield();
            waited += Time.unscaledDeltaTime;

            if (this == null || !isActiveAndEnabled) return;   // closed while waiting
        }

        if (DatabaseManager.Instance == null)
        {
            Debug.LogError("UserProfileUI: DatabaseManager was not found. Start from the first scene, "
                           + "or check that it is a root object in the scene.");
            return;
        }

        FirebaseUser authUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (authUser == null)
        {
            Debug.LogWarning("UserProfileUI: no logged-in user.");
            return;
        }

        UserData user = await DatabaseManager.Instance.GetUserAsync(authUser.UserId);

        if (this == null || !isActiveAndEnabled) return;       // closed while loading

        if (user == null)
        {
            Debug.LogWarning("UserProfileUI: no profile document found for this user.");
            return;
        }

        SetText(nameText, user.name);
        SetText(idText, user.userID);
        SetText(emailText, user.email);
        SetText(attemptCount, user.attemptCount.ToString());

        SetSlider(safetySlider, user.AverageSafety);
        SetSlider(competencySlider, user.AverageCompetency);
        SetSlider(proceduralSlider, user.AverageProcedural);
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target != null) target.text = value;
    }

    private void SetSlider(Slider slider, float value)
    {
        if (slider != null) slider.value = value;
    }
}