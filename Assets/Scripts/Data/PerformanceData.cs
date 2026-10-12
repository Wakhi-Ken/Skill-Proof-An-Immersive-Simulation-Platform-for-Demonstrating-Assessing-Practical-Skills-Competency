using System.Collections.Generic;
using Firebase.Firestore;

[FirestoreData]
public class PerformanceData
{
    [FirestoreProperty] public string performanceID { get; set; }
    [FirestoreProperty] public string simulationID { get; set; }
    [FirestoreProperty] public string simulationName { get; set; }
    [FirestoreProperty] public Timestamp startedAt { get; set; }
    [FirestoreProperty] public Timestamp completedAt { get; set; }
    [FirestoreProperty] public double completionTime { get; set; }   // seconds
    [FirestoreProperty] public int errors { get; set; }
    [FirestoreProperty] public bool allTasksCompleted { get; set; }

    // Scores are 0-100 so they map straight onto sliders
    [FirestoreProperty] public double safetyScore { get; set; }
    [FirestoreProperty] public double competencyScore { get; set; }
    [FirestoreProperty] public double proceduralScore { get; set; }
    [FirestoreProperty] public double accuracy { get; set; }
    [FirestoreProperty] public double compliance { get; set; }

    [FirestoreProperty] public bool competent { get; set; }
    [FirestoreProperty] public string competencyLevel { get; set; }
    [FirestoreProperty] public string feedbackText { get; set; }
    [FirestoreProperty] public List<ActivityResult> activities { get; set; }

    [FirestoreProperty] public string videoPath { get; set; }        // local Quest path
    [FirestoreProperty] public string certificatePath { get; set; }  // Storage path
}