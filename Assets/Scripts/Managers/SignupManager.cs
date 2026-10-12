using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;

public class SignupManager : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField nameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPassword;

    [Header("UI")]
    public TMP_Text messageText;
    public GameObject signUpCanvas;
    public GameObject loginCanvas;

    private FirebaseAuth auth;
    private bool isBusy = false;

    private const int MinPasswordLength = 6;

    public void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        // Make sure the password fields show dots instead of the text
        passwordInput.contentType = TMP_InputField.ContentType.Password;
        confirmPassword.contentType = TMP_InputField.ContentType.Password;
        passwordInput.ForceLabelUpdate();
        confirmPassword.ForceLabelUpdate();
    }

    public void SignUp()
    {
        if (isBusy) return; // stops double-clicks creating two requests

        string name = nameInput.text.Trim();
        string email = emailInput.text.Trim();
        string password = passwordInput.text;
        string confirmPasswordText = confirmPassword.text;

        // Validation
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

        if (password.Length < MinPasswordLength)
        {
            messageText.text = $"Password must be at least {MinPasswordLength} characters.";
            return;
        }

        if (!HasLetterAndDigit(password))
        {
            messageText.text = "Password must contain at least one letter and one number.";
            return;
        }

        if (password != confirmPasswordText)
        {
            messageText.text = "Passwords do not match.";
            return;
        }

        // Create account
        isBusy = true;
        messageText.text = "Creating account...";

        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                // Clear the passwords from the screen whatever the result
                ClearPasswordFields();

                if (task.IsCanceled)
                {
                    isBusy = false;
                    messageText.text = "Account creation canceled.";
                    return;
                }

                if (task.IsFaulted)
                {
                    isBusy = false;
                    messageText.text = GetErrorMessage(task.Exception);
                    Debug.LogError(task.Exception);
                    return;
                }

                FirebaseUser user = task.Result.User;
                Debug.Log("Account created successfully! UID: " + user.UserId);

                UserProfile profile = new UserProfile
                {
                    DisplayName = name
                };

                user.UpdateUserProfileAsync(profile)
                    .ContinueWithOnMainThread(profileTask =>
                    {
                        if (profileTask.IsFaulted)
                        {
                            Debug.LogError(profileTask.Exception);
                            // The account exists, so continue anyway
                        }
                        else
                        {
                            Debug.Log("Profile updated successfully!");
                        }

                        // Save the user to Firestore BEFORE signing out
                        SaveProfileToDatabase(user.UserId, name, email);
                    });
            });
    }

    // Database step

    private void SaveProfileToDatabase(string uid, string name, string email)
    {
        if (DatabaseManager.Instance == null)
        {
            Debug.LogError("DatabaseManager is missing from the scene.");
            FinishSignUp(false);
            return;
        }

        messageText.text = "Setting up your profile...";

        DatabaseManager.Instance.CreateUserProfileAsync(uid, name, email)
            .ContinueWithOnMainThread(dbTask =>
            {
                bool saved = !dbTask.IsFaulted && !dbTask.IsCanceled && dbTask.Result;
                FinishSignUp(saved);
            });
    }

    private void FinishSignUp(bool profileSaved)
    {
        isBusy = false;

        // Sign out the user after account creation to force them to log in
        auth.SignOut();

        messageText.text = profileSaved
            ? "Account created! Please log in."
            : "Account created! Please log in. Your profile will finish setting up when you log in.";

        signUpCanvas.SetActive(false);
        loginCanvas.SetActive(true);
    }

    // Helpers

    private void ClearPasswordFields()
    {
        passwordInput.text = "";
        confirmPassword.text = "";
    }

    private bool HasLetterAndDigit(string text)
    {
        bool hasLetter = false;
        bool hasDigit = false;

        foreach (char c in text)
        {
            if (char.IsLetter(c)) hasLetter = true;
            if (char.IsDigit(c)) hasDigit = true;
        }

        return hasLetter && hasDigit;
    }

    private string GetErrorMessage(System.AggregateException exception)
    {
        FirebaseException firebaseEx = exception?.GetBaseException() as FirebaseException;

        if (firebaseEx == null)
            return "Could not create account. Please try again.";

        AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

        switch (errorCode)
        {
            case AuthError.EmailAlreadyInUse:
                return "This email is already registered.";
            case AuthError.InvalidEmail:
                return "Please enter a valid email address.";
            case AuthError.WeakPassword:
                return "Password is too weak.";
            case AuthError.NetworkRequestFailed:
                return "No internet connection. Please check your network.";
            default:
                return "Could not create account. Please try again.";
        }
    }
}