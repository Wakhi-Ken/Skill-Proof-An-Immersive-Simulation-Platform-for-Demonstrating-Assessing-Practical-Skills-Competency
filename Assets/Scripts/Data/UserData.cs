using Firebase.Firestore;


[FirestoreData]
public class UserData
{
    [FirestoreProperty] public string userID { get; set; }
    [FirestoreProperty] public string name { get; set; }
    [FirestoreProperty] public string email { get; set; }
    [FirestoreProperty] public int attemptCount { get; set; }
    [FirestoreProperty] public double sumSafety { get; set; }
    [FirestoreProperty] public double sumCompetency { get; set; }
    [FirestoreProperty] public double sumProcedural { get; set; }

    // Not stored, calculated when needed (no attribute = ignored by Firestore)
    public float AverageSafety => attemptCount == 0 ? 0f : (float)(sumSafety / attemptCount);
    public float AverageCompetency => attemptCount == 0 ? 0f : (float)(sumCompetency / attemptCount);
    public float AverageProcedural => attemptCount == 0 ? 0f : (float)(sumProcedural / attemptCount);
}