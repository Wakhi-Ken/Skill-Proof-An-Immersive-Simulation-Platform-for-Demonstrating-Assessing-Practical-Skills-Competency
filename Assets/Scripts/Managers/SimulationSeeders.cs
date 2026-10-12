using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine;

public class SimulationSeeder : MonoBehaviour
{
    [ContextMenu("Seed All Simulations")]
    public async void SeedAll()
    {
        Dictionary<string, SimulationData> simulations = BuildSimulations();

        try
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            WriteBatch batch = db.StartBatch();

            foreach (KeyValuePair<string, SimulationData> entry in simulations)
            {
                DocumentReference docRef = db.Collection("simulations").Document(entry.Key);
                batch.Set(docRef, entry.Value);
            }

            await batch.CommitAsync();
            Debug.Log("Seeded " + simulations.Count + " simulations.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Seeding failed: " + e.Message);
        }
    }

    private Dictionary<string, SimulationData> BuildSimulations()
    {
        Dictionary<string, SimulationData> result = new Dictionary<string, SimulationData>();

        result.Add("electrical_wiring", new SimulationData
        {
            name = "Electrical Wiring Installation",
            description = "Practice safe installation of a household wiring circuit in VR.",
            activities = new List<ActivityInfo>
            {
                new ActivityInfo { activityID = "activity_1", name = "Activity 1 name", instructions = "What the player must do" },
                new ActivityInfo { activityID = "activity_2", name = "Activity 2 name", instructions = "What the player must do" },
                new ActivityInfo { activityID = "activity_3", name = "Activity 3 name", instructions = "What the player must do" },
                new ActivityInfo { activityID = "activity_4", name = "Activity 4 name", instructions = "What the player must do" },
                new ActivityInfo { activityID = "activity_5", name = "Activity 5 name", instructions = "What the player must do" }
            }
        });

        result.Add("solar_panel_installation", new SimulationData
        {
            name = "Solar Panel Installation",
            description = "Practice safe installation of a solar panel system in VR.",
            activities = new List<ActivityInfo>
            {
                new ActivityInfo { activityID = "activity_1", name = "Activity 1 name", instructions = "What the player must do" },
                new ActivityInfo { activityID = "activity_2", name = "Activity 2 name", instructions = "What the player must do" },
                new ActivityInfo { activityID = "activity_3", name = "Activity 3 name", instructions = "What the player must do" },
                new ActivityInfo { activityID = "activity_4", name = "Activity 4 name", instructions = "What the player must do" },
                new ActivityInfo { activityID = "activity_5", name = "Activity 5 name", instructions = "What the player must do" }
            }
        });

        return result;
    }
}