using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserProfileUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text idText;
    [SerializeField] private TMP_Text emailText;

    [SerializeField] private Slider safetySlider;
    [SerializeField] private Slider competencySlider;
    [SerializeField] private Slider proceduralSlider;

    private async void OnEnable()
    {
        FirebaseUser authUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (authUser == null) return;

        UserData user = await DatabaseManager.Instance.GetUserAsync(authUser.UserId);
        if (user == null) return;

        nameText.text = user.name;
        idText.text = user.userID;
        emailText.text = user.email;

        safetySlider.value = user.AverageSafety;
        competencySlider.value = user.AverageCompetency;
        proceduralSlider.value = user.AverageProcedural;
    }
}