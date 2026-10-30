using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

//save the PDF evidence and send it to the user's email

[Serializable]
class EmailPayload
{
    public string secret, to, name, simulation, fileName, pdfBase64;
    public string competent, completionStatus, date, timeUsed;
    public int activitiesDone, activitiesTotal, overall, safety, errors;
}

public class EvidenceSaver : MonoBehaviour
{
    [SerializeField] private DetailedGeneratedUI detailsUI;
    // the Google Apps Script endpoint that sends the email
    [SerializeField] private string endpointUrl = "https://script.google.com/macros/s/AKfycbwp9shHPhV8TO4uOS6zhnxT8ag06v_3_yPSBEf8888A-jWXZc03eikLGKf3NGHHCa3m/exec";
    [SerializeField] private string secret = "Evidence_Skill_Proof";
    [SerializeField] private Button saveButton;
    [SerializeField] private TMP_Text statusText;

    [Header("PDF look")]
    [SerializeField] private Texture2D logoTexture;          
    private const int PdfBrand = 0x0B3C5D;                  
    private bool busy;

    // evidence button clicked: save the PDF and send it to the user's email
    public void OnSaveEvidenceClicked()
    {
        if (busy) return;

        PerformanceData p = detailsUI != null ? detailsUI.CurrentPerformance : null;
        if (p == null)
        {
            SetStatus("Results are still loading. Try again in a moment.");
            return;
        }
        StartCoroutine(SaveAndSend(p, detailsUI.CurrentUser));
    }

    private IEnumerator SaveAndSend(PerformanceData p, UserData user)
    {
        busy = true;
        if (saveButton) saveButton.interactable = false;
        SetStatus("Creating PDF...");

        FirebaseUser auth = FirebaseAuth.DefaultInstance.CurrentUser;
        string name = !string.IsNullOrEmpty(user?.name) ? user.name : auth?.DisplayName ?? "";
        string id = user != null ? user.userID : auth?.UserId ?? "";
        string email = !string.IsNullOrEmpty(user?.email) ? user.email : auth?.Email ?? "";

        int done = 0, total = p.activities != null ? p.activities.Count : 0;
        if (p.activities != null) foreach (var a in p.activities) if (a.completed) done++;

        int secs = Mathf.RoundToInt((float)p.completionTime);
        float safety = (float)p.safetyScore;
        float overall = (safety + (float)p.competencyScore
                         + (float)p.proceduralScore + (float)p.compliance) / 4f;

        string dateText = p.completedAt.ToDateTime().ToLocalTime().ToString("dd MMM yyyy, HH:mm");
        string timeText = $"{secs / 60:00}:{secs % 60:00}";
        string statusLine = p.allTasksCompleted ? "Completed" : $"Incomplete ({total - done} not completed)";

        var report = new PdfReportData
        {
            name = name,
            email = email,
            simulation = p.simulationName,
            date = dateText,
            timeUsed = timeText,
            completionStatus = statusLine,
            competent = p.competent,
            activitiesDone = done,
            activitiesTotal = total,
            overall = Mathf.RoundToInt(overall),
            safety = Mathf.RoundToInt(safety),
            errors = p.errors,
            feedback = p.feedbackText
        };
        if (p.activities != null)
            foreach (var act in p.activities)
                report.activities.Add(new PdfActivityRow { name = act.name, completed = act.completed, errors = act.errors });

        byte[] logoJpg = null;
        int logoW = 0, logoH = 0;
        if (logoTexture != null) logoJpg = ToJpg(logoTexture, out logoW, out logoH);

        byte[] pdf = EvidencePdf.Build(report, logoJpg, logoW, logoH, PdfBrand);

        string fileName = $"SkillProof_{p.simulationName}_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
                          .Replace(" ", "_");
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(path, pdf);
        Debug.Log("PDF saved: " + path);

        SetStatus("Saved. Sending email...");
        string json = JsonUtility.ToJson(new EmailPayload
        {
            secret = secret,
            to = email,
            name = name,
            simulation = p.simulationName,
            fileName = fileName,
            pdfBase64 = Convert.ToBase64String(pdf),
            competent = p.competent ? "true" : "false",
            completionStatus = statusLine,
            date = dateText,
            timeUsed = timeText,
            activitiesDone = done,
            activitiesTotal = total,
            overall = Mathf.RoundToInt(overall),
            safety = Mathf.RoundToInt(safety),
            errors = p.errors
        });

        Debug.Log("Sending evidence email to: " + email);

        using (var req = new UnityWebRequest(endpointUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "text/plain");
            req.redirectLimit = 0;
            yield return req.SendWebRequest();

            long code = req.responseCode;
            string location = req.GetResponseHeader("Location");
            string reply = req.downloadHandler.text ?? "";

            if ((code == 301 || code == 302 || code == 303) && !string.IsNullOrEmpty(location))
            {
                using (var get = UnityWebRequest.Get(location))
                {
                    yield return get.SendWebRequest();
                    reply = get.downloadHandler.text ?? "";
                }
            }

            Debug.Log($"Email POST code:{code} reply:{reply.Substring(0, Mathf.Min(300, reply.Length))}");

            SetStatus(reply.Contains("\"ok\":true")
                ? "Evidence saved and emailed to " + email
                : "PDF saved, but the email failed. See the Console.");
        }

        busy = false;
        if (saveButton) saveButton.interactable = true;
    }

    // Converts the logo to a small JPEG (works even if the texture isn't Read/Write enabled)
    private byte[] ToJpg(Texture2D src, out int w, out int h)
    {
        float scale = Mathf.Min(1f, 400f / Mathf.Max(src.width, src.height));
        w = Mathf.Max(1, Mathf.RoundToInt(src.width * scale));
        h = Mathf.Max(1, Mathf.RoundToInt(src.height * scale));

        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(src, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        byte[] jpg = tex.EncodeToJPG(90);
        Destroy(tex);
        return jpg;
    }

    private void SetStatus(string msg) { if (statusText) statusText.text = msg; }
}