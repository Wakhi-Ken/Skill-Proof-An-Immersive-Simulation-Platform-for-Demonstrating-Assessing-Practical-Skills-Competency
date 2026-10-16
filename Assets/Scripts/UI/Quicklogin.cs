using TMPro;
using UnityEngine;

// Testing helper: fills the login fields only. Remove before sharing the project.
public class Quicklogin : MonoBehaviour
{
    [Header("Login fields")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Test account")]
    [SerializeField] private string email = "kidsavag3s@gmail.com";
    [SerializeField] private string password = "123456son";

    // Hook this to the "Quick Login" button
    public void FillFields()
    {
        if (emailInput == null || passwordInput == null)
        {
            Debug.LogWarning("Quicklogin: assign the email and password input fields.");
            return;
        }

        // Clear a field only if it already has text in it
        if (!string.IsNullOrEmpty(emailInput.text)) emailInput.text = "";
        if (!string.IsNullOrEmpty(passwordInput.text)) passwordInput.text = "";

        emailInput.text = email;
        passwordInput.text = password;
    }
}