using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine.SceneManagement;

public class SignupManager : MonoBehaviour
{

    public TMP_InputField nameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPassword;

    public TMP_Text messageText;
    public string nextSceneName = "Start Menu";

    private FirebaseAuth auth;


    public void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
    }

    public void SignUp()
    {
        string name = nameInput.text.Trim();
        string email = emailInput.text.Trim();
        string password = passwordInput.text;
        string confirmPasswordText = confirmPassword.text;
        if (string.IsNullOrEmpty(name))
        {
            messageText.text = "Please enter your name.";
            return;
        }

        if (string.IsNullOrEmpty(email))
        {
            messageText.text = "Please enter your email.";
            return;
        }

        if (password.Length < 6)
        {
            messageText.text = "Password must be at least 6 characters.";
            return;
        }

        if (password != confirmPasswordText)
        {
            messageText.text = "Passwords do not match.";
            return;
        }

        messageText.text = "Creating Account...";

        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                messageText.text = "Account creation canceled.";
                return;
            }
            if (task.IsFaulted)
            {
                messageText.text = "Could not create account. Please try again.";
                Debug.LogError(task.Exception);
                return;
            }

            FirebaseUser User = task.Result.User;
            Debug.Log("Account created successfully!");
            Debug.Log("UID: " + User.UserId);

            UserProfile profile = new UserProfile
            {
                DisplayName = name
            };

            User.UpdateUserProfileAsync(profile)
            .ContinueWithOnMainThread(profileTask =>
            {
                if (profileTask.IsFaulted)
                {
                    Debug.LogError(profileTask.Exception);
                    return;
                }

                Debug.Log("Profile updated successfully!");
                SceneManager.LoadScene(nextSceneName);
            });
        });
    }
}
