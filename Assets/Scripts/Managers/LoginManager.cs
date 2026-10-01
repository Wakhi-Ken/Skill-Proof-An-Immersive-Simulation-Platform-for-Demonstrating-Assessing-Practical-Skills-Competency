using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    public TMP_Text messageText;

    public string nextSceneName = "Start Menu";

    FirebaseAuth auth;

    //login manager
    public void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
    }

    public void Login()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email))
        {
            messageText.text = "Please enter your email.";
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            messageText.text = "Please enter your password.";
            return;
        }

        messageText.text = "logining in...";

        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                messageText.text = "login canceled.";
                return;
            }
            if (task.IsFaulted)
            {
                messageText.text = "Invalid email or password ";
                Debug.LogError(task.Exception);
                return;
            }

            FirebaseUser user = task.Result.User;
            Debug.Log("Login successfull!");
            Debug.Log("User ID: " + user.UserId);
            Debug.Log("User Email: " + user.Email);

            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        });
    }
}
