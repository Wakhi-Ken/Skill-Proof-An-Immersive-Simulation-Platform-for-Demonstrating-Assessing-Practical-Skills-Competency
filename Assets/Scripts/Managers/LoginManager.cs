using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro;

public class LoginManager : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    [Header("UI")]
    public TMP_Text messageText;
    public GameObject loginCanvas;
    public GameObject SimulatorCanvas;

    private bool isBusy = false;

    private void Start()
    {
        // Show dots instead of the password text
        passwordInput.contentType = TMP_InputField.ContentType.Password;
        passwordInput.ForceLabelUpdate();
    }

    public void Login()
    {
        if (isBusy) return; // stops double-taps sending two requests

        if (!FirebaseManager.IsReady)
        {
            messageText.text = "Please wait, Firebase is starting...";
            return;
        }

        FirebaseAuth auth = FirebaseManager.Auth;

        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        // ---------- Validation ----------
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

        // ---------- Sign in ----------
        isBusy = true;
        messageText.text = "Logging in...";

        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                isBusy = false;

                // Clear the password whatever the result
                passwordInput.text = "";

                if (task.IsCanceled)
                {
                    messageText.text = "Login canceled.";
                    return;
                }

                if (task.IsFaulted)
                {
                    messageText.text = GetErrorMessage(task.Exception);
                    Debug.LogError(task.Exception);
                    return;
                }

                FirebaseUser user = task.Result.User;
                Debug.Log("Login successful! UID: " + user.UserId);

                messageText.text = "";
                emailInput.text = "";

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
        }

        FirebaseManager.Auth.SignOut();
        Debug.Log("User logged out.");

        SimulatorCanvas.SetActive(false);
        loginCanvas.SetActive(true);

        emailInput.text = "";
        passwordInput.text = "";
        messageText.text = "";
    }

    // ---------- Helpers ----------

    private string GetErrorMessage(System.AggregateException exception)
    {
        FirebaseException firebaseEx = exception?.GetBaseException() as FirebaseException;

        if (firebaseEx == null)
            return "Could not log in. Please try again.";

        AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

        switch (errorCode)
        {
            case AuthError.InvalidEmail:
                return "Please enter a valid email address.";
            case AuthError.WrongPassword:
            case AuthError.UserNotFound:
            case AuthError.InvalidCredential:
                // Same message for both, so attackers can't tell which emails exist
                return "Invalid email or password.";
            case AuthError.UserDisabled:
                return "This account has been disabled.";
            case AuthError.TooManyRequests:
                return "Too many attempts. Please wait a moment and try again.";
            case AuthError.NetworkRequestFailed:
                return "No internet connection. Please check your network.";
            default:
                return "Could not log in. Please try again.";
        }
    }
}