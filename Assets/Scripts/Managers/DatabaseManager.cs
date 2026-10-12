using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }

    // Only use this after FirebaseManager.IsReady is true
    private FirebaseFirestore Db => FirebaseFirestore.DefaultInstance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Users

    // Call right after Firebase Auth creates the account
    public async Task<bool> CreateUserProfileAsync(string uid, string name, string email)
    {
        UserData user = new UserData
        {
            userID = uid,
            name = name,
            email = email,
            attemptCount = 0,
            sumSafety = 0,
            sumCompetency = 0,
            sumProcedural = 0
        };

        try
        {
            await Db.Collection("users").Document(uid).SetAsync(user);
            Debug.Log("User profile created.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("CreateUserProfileAsync failed: " + e.Message);
            return false;
        }
    }

    // Call after a successful login, so any account without a profile gets one
    public async Task EnsureUserProfileAsync(FirebaseUser authUser)
    {
        UserData existing = await GetUserAsync(authUser.UserId);
        if (existing != null) return;

        string name = string.IsNullOrEmpty(authUser.DisplayName)
            ? authUser.Email
            : authUser.DisplayName;

        await CreateUserProfileAsync(authUser.UserId, name, authUser.Email);
    }

    public async Task<UserData> GetUserAsync(string uid)
    {
        try
        {
            DocumentSnapshot snap = await Db.Collection("users").Document(uid).GetSnapshotAsync();
            return snap.Exists ? snap.ConvertTo<UserData>() : null;
        }
        catch (Exception e)
        {
            Debug.LogError("GetUserAsync failed: " + e.Message);
            return null;
        }
    }

    // Simulations

    public async Task<SimulationData> GetSimulationAsync(string simulationId)
    {
        try
        {
            DocumentSnapshot snap = await Db.Collection("simulations")
                .Document(simulationId).GetSnapshotAsync();
            return snap.Exists ? snap.ConvertTo<SimulationData>() : null;
        }
        catch (Exception e)
        {
            Debug.LogError("GetSimulationAsync failed: " + e.Message);
            return null;
        }
    }

    // ---------- Performances ----------

    // Saves one finished simulation AND updates the profile averages together
    public async Task<bool> SavePerformanceAsync(PerformanceData p)
    {
        FirebaseUser authUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (authUser == null)
        {
            Debug.LogError("No logged-in user.");
            return false;
        }

        try
        {
            DocumentReference userRef = Db.Collection("users").Document(authUser.UserId);
            DocumentReference perfRef = userRef.Collection("performances").Document();
            p.performanceID = perfRef.Id;

            Dictionary<string, object> totals = new Dictionary<string, object>
            {
                { "attemptCount", FieldValue.Increment(1) },
                { "sumSafety", FieldValue.Increment(p.safetyScore) },
                { "sumCompetency", FieldValue.Increment(p.competencyScore) },
                { "sumProcedural", FieldValue.Increment(p.proceduralScore) }
            };

            WriteBatch batch = Db.StartBatch();
            batch.Set(perfRef, p);
            batch.Update(userRef, totals);
            await batch.CommitAsync();

            Debug.Log("Performance saved: " + p.performanceID);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("SavePerformanceAsync failed: " + e.Message);
            return false;
        }
    }

    public async Task<List<PerformanceData>> GetPerformancesAsync(string uid)
    {
        List<PerformanceData> results = new List<PerformanceData>();
        try
        {
            QuerySnapshot snap = await Db.Collection("users").Document(uid)
                .Collection("performances")
                .OrderByDescending("completedAt")
                .GetSnapshotAsync();

            foreach (DocumentSnapshot doc in snap.Documents)
                results.Add(doc.ConvertTo<PerformanceData>());
        }
        catch (Exception e)
        {
            Debug.LogError("GetPerformancesAsync failed: " + e.Message);
        }
        return results;
    }
}