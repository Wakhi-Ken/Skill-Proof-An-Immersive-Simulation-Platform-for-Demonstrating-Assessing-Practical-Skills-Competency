using UnityEngine;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    public TMP_Text messageText;

    public GameObject loginCanvas;
    public GameObject SimulatorCanvas;

    public void Login()
    {
        if (!FirebaseManager.IsReady)
        {
            messageText.text = "Please wait, Firebase is starting...";
            return;
        }

        FirebaseAuth auth = FirebaseManager.Auth;

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

        messageText.text = "Logging in...";

        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    messageText.text = "Login canceled.";
                    return;
                }

                if (task.IsFaulted)
                {
                    messageText.text = "Invalid email or password.";
                    Debug.LogError(task.Exception);
                    return;
                }

                FirebaseUser user = task.Result.User;

                Debug.Log("Login successful!");
                Debug.Log("User ID: " + user.UserId);
                Debug.Log("User Email: " + user.Email);

                loginCanvas.SetActive(false);
                SimulatorCanvas.SetActive(true);
            });
    }
    public void Logout()
    {
        if (!FirebaseManager.IsReady)
        {
            messageText.text = "Firebase is not ready.";
            return;
        } FirebaseAuth auth = FirebaseManager.Auth;
        auth.SignOut(); Debug.Log("User logged out.");
        SimulatorCanvas.SetActive(false); loginCanvas.SetActive(true);
        emailInput.text = ""; passwordInput.text = ""; messageText.text = ""; }
}