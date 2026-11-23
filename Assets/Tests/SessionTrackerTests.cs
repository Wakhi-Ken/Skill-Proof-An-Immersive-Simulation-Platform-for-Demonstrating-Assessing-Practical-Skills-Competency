using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SessionTrackerTests
{
    GameObject go;
    SessionTracker tracker;

    // Sets a property or field that is private in SessionTracker
    void SetProp(string name, object value)
    {
        typeof(SessionTracker).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .GetSetMethod(true).Invoke(tracker, new[] { value });
    }

    void SetField(string name, object value)
    {
        typeof(SessionTracker).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tracker, value);
    }

    PerformanceData Build()
    {
        return (PerformanceData)typeof(SessionTracker)
            .GetMethod("BuildPerformance", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(tracker, null);
    }

    void CompleteAllInOrder()
    {
        for (int i = 1; i <= 5; i++) tracker.CompleteActivity("activity_" + i);
    }

    [SetUp]
    public void Setup()
    {
        go = new GameObject("tracker");
        tracker = go.AddComponent<SessionTracker>();
        tracker.autoFinishWhenAllDone = false;        // do not save to Firestore in tests

        SetProp("IsRunning", true);
        SetProp("Simulation", new SimulationData { name = "Test simulation" });
        SetField("startedAtUtc", DateTime.UtcNow);    // avoids an invalid default date

        tracker.Results.Clear();
        for (int i = 1; i <= 5; i++)
            tracker.Results.Add(new ActivityResult
            {
                activityID = "activity_" + i,
                name = "Activity " + i,
                completed = false,
                errors = 0
            });
    }

    [TearDown]
    public void Teardown() { UnityEngine.Object.DestroyImmediate(go); }

    // UT-01
    [Test]
    public void ZeroErrors_GivesFullSafetyScore()
    {
        Assert.AreEqual(100f, tracker.SafetyScore);
    }

    // UT-02
    [Test]
    public void SafetyViolation_LowersSafetyScore()
    {
        tracker.RegisterError("activity_1", true);
        Assert.AreEqual(1, tracker.SafetyViolations);
        Assert.AreEqual(1, tracker.TotalErrors);
        Assert.AreEqual(90f, tracker.SafetyScore);
    }

    // UT-03
    [Test]
    public void OneActivityDone_IsNotCompetent()
    {
        tracker.CompleteActivity("activity_1");
        PerformanceData p = Build();
        Assert.IsFalse(p.competent);
        Assert.AreEqual("Not Yet Competent", p.competencyLevel);
    }

    [Test]
    public void AllActivitiesDoneWithNoErrors_IsCompetent()
    {
        CompleteAllInOrder();
        PerformanceData p = Build();
        Assert.IsTrue(p.competent);
        Assert.AreEqual("Advanced", p.competencyLevel);
    }

    [Test]
    public void AllDoneButTooManyErrors_IsNotCompetent()
    {
        CompleteAllInOrder();
        for (int i = 0; i < 6; i++) tracker.RegisterError("", false);   // limit is 5
        Assert.IsFalse(Build().competent);
    }

    // UT-04
    [Test]
    public void FourOfFiveDone_AllTasksNotCompleted()
    {
        for (int i = 1; i <= 4; i++) tracker.CompleteActivity("activity_" + i);
        Assert.AreEqual(4, tracker.CompletedCount);
        Assert.IsFalse(Build().allTasksCompleted);
    }

    // UT-06
    [Test]
    public void RemovedComponent_ActivityBecomesIncomplete()
    {
        tracker.CompleteActivity("activity_1");
        Assert.AreEqual(1, tracker.CompletedCount);
        tracker.UncompleteActivity("activity_1");
        Assert.AreEqual(0, tracker.CompletedCount);
    }

    [Test]
    public void ActivityDoneOutOfOrder_CountsAsError()
    {
        tracker.CompleteActivity("activity_2");   // activity_1 is not done yet
        Assert.AreEqual(1, tracker.TotalErrors);
    }

    [Test]
    public void NoRunningSession_IgnoresActivity()
    {
        SetProp("IsRunning", false);
        tracker.CompleteActivity("activity_1");
        Assert.AreEqual(0, tracker.CompletedCount);
    }
}