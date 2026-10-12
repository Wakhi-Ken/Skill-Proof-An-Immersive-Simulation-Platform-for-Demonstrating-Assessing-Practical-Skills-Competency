using Firebase.Firestore;

[FirestoreData]
public class ActivityResult
{
    [FirestoreProperty] public string activityID { get; set; }
    [FirestoreProperty] public string name { get; set; }
    [FirestoreProperty] public bool completed { get; set; }
    [FirestoreProperty] public int errors { get; set; }
}