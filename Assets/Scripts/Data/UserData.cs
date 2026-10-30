using Firebase.Firestore;

[FirestoreData]

// Represents a user profile in the Firestore database.
public class UserData
{
   
    [FirestoreProperty] public string userID { get; set; }
    [FirestoreProperty] public string name { get; set; }
    [FirestoreProperty] public string email { get; set; }
    [FirestoreProperty] public int attemptCount { get; set; }

    // Latest finished session. Overwritten every time, never added to.
    [FirestoreProperty] public double latestSafety { get; set; }
    [FirestoreProperty] public double latestCompetency { get; set; }
    [FirestoreProperty] public double latestProcedural { get; set; }

    // Old running totals. No longer used, kept so existing accounts still load.
    [FirestoreProperty] public double sumSafety { get; set; }
    [FirestoreProperty] public double sumCompetency { get; set; }
    [FirestoreProperty] public double sumProcedural { get; set; }

    // Same names the profile UI uses, now returning the latest session's score (0-100)
    public float AverageSafety => (float)latestSafety;
    public float AverageCompetency => (float)latestCompetency;
    public float AverageProcedural => (float)latestProcedural;
}