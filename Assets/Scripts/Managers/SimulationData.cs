using System.Collections.Generic;
using Firebase.Firestore;

[FirestoreData]
public class ActivityInfo
{
    [FirestoreProperty] public string activityID { get; set; }
    [FirestoreProperty] public string name { get; set; }
    [FirestoreProperty] public string instructions { get; set; }
}

[FirestoreData]
public class SimulationData
{
    [FirestoreProperty] public string name { get; set; }
    [FirestoreProperty] public string description { get; set; }
    [FirestoreProperty] public List<ActivityInfo> activities { get; set; }
}