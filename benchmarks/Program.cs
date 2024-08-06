using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;

public enum ActionType : byte
{
  Action1,
  Action2,
  Action3,
}

public struct ScheduledAction
{
  public ActionType Type;
  public long StartTime;
  public long EndTime;
  public int Occurrences;
  public int RemainingOccurrences;
}

public interface Scheduler
{
  public void ScheduleAction(ActionType action, long startTime, long endTime, int occurances);
  public void GetActionsForTime(long time, Action<ActionType> callback);
  public long? GetNextActionTime();
}

public class HighPerformanceActionScheduler : Scheduler
{
  private const int InitialCapacity = 1024;
  private ScheduledAction[] _actions;
  private int _count;
  private long _currentTime;

  public HighPerformanceActionScheduler()
  {
    _actions = new ScheduledAction[InitialCapacity];
    _count = 0;
    _currentTime = 0;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void ScheduleAction(ActionType action, long startTime, long endTime, int occurrences)
  {
    if (_count == _actions.Length)
    {
      Console.WriteLine("[WARN] Resizing action array");
      Array.Resize(ref _actions, _actions.Length * 2);
    }

    _actions[_count++] = new ScheduledAction
    {
      Type = action,
      StartTime = startTime,
      EndTime = endTime,
      Occurrences = occurrences,
      RemainingOccurrences = occurrences
    };
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void GetActionsForTime(long time, Action<ActionType> callback)
  {
    _currentTime = time;
    int writeIndex = 0;

    for (int i = 0; i < _count; i++)
    {
      ref var action = ref _actions[i];
      if (action.StartTime <= time && time <= action.EndTime && action.RemainingOccurrences > 0)
      {
        callback(action.Type);
        action.RemainingOccurrences--;

        if (action.RemainingOccurrences > 0)
        {
          _actions[writeIndex++] = action;
        }
      }
      else if (action.StartTime > time || (action.StartTime <= time && action.RemainingOccurrences > 0))
      {
        _actions[writeIndex++] = action;
      }
    }

    _count = writeIndex;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public long? GetNextActionTime()
  {
    long? nextTime = null;

    for (int i = 0; i < _count; i++)
    {
      ref var action = ref _actions[i];
      if (action.StartTime > _currentTime && action.RemainingOccurrences > 0)
      {
        nextTime = nextTime.HasValue ? Math.Min(nextTime.Value, action.StartTime) : action.StartTime;
      }
    }

    return nextTime;
  }
}

public class RevisedActionScheduler : Scheduler
{
  private const int InitialCapacity = 1024;
  private ScheduledAction[] _actions;
  private int _count;
  private long _currentTime;
  private long _earliestFutureTime;

  public RevisedActionScheduler()
  {
    _actions = new ScheduledAction[InitialCapacity];
    _count = 0;
    _currentTime = 0;
    _earliestFutureTime = long.MaxValue;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void ScheduleAction(ActionType action, long startTime, long endTime, int occurrences)
  {
    if (_count == _actions.Length)
    {
      Console.WriteLine("[WARN] Resizing action array");
      Array.Resize(ref _actions, _actions.Length * 2);
    }

    _actions[_count++] = new ScheduledAction
    {
      Type = action,
      StartTime = startTime,
      EndTime = endTime,
      Occurrences = occurrences,
      RemainingOccurrences = occurrences
    };

    _earliestFutureTime = Math.Min(_earliestFutureTime, startTime);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void GetActionsForTime(long time, Action<ActionType> callback)
  {
    if (_count == 0 || time < _earliestFutureTime)
      return;

    _currentTime = time;
    int writeIndex = 0;
    bool updatedEarliestFutureTime = false;
    long newEarliestFutureTime = long.MaxValue;

    for (int i = 0; i < _count; i++)
    {
      ref var action = ref _actions[i];
      if (action.StartTime <= time && time <= action.EndTime && action.RemainingOccurrences > 0)
      {
        callback(action.Type);
        action.RemainingOccurrences--;

        if (action.RemainingOccurrences > 0)
        {
          _actions[writeIndex++] = action;
          newEarliestFutureTime = Math.Min(newEarliestFutureTime, action.StartTime);
          updatedEarliestFutureTime = true;
        }
      }
      else if (action.StartTime > time)
      {
        _actions[writeIndex++] = action;
        newEarliestFutureTime = Math.Min(newEarliestFutureTime, action.StartTime);
        updatedEarliestFutureTime = true;
      }
    }

    _count = writeIndex;
    if (updatedEarliestFutureTime)
    {
      _earliestFutureTime = newEarliestFutureTime;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public long? GetNextActionTime()
  {
    return _count > 0 ? _earliestFutureTime : null;
  }
}

public class TimelessScheduler : Scheduler
{
  private const int InitialCapacity = 1024;
  private ScheduledAction[] _actions;
  private int _count;
  private long _nextTime;
  private bool _nextTimeStale = true;

  public TimelessScheduler()
  {
    _actions = new ScheduledAction[InitialCapacity];
    _count = 0;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void ScheduleAction(ActionType action, long startTime, long endTime, int occurrences)
  {
    if (_count == _actions.Length)
    {
      Console.WriteLine("[WARN] Resizing action array");
      Array.Resize(ref _actions, _actions.Length * 2);
    }

    _actions[_count++] = new ScheduledAction
    {
      Type = action,
      StartTime = startTime,
      EndTime = endTime,
      Occurrences = occurrences,
      RemainingOccurrences = occurrences
    };

    if (startTime < _nextTime)
    {
      _nextTime = startTime;
      _nextTimeStale = false;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void GetActionsForTime(long time, Action<ActionType> callback)
  {
    if (_count == 0)
      return;

    int writeIndex = 0;
    for (int i = 0; i < _count; i++)
    {
      ref var action = ref _actions[i];
      if (action.StartTime <= time && time <= action.EndTime && action.RemainingOccurrences > 0)
      {
        callback(action.Type);
        action.RemainingOccurrences--;

        if (action.RemainingOccurrences > 0)
        {
          _actions[writeIndex++] = action;
        }
      }
      else if (action.StartTime > time)
      {
        _actions[writeIndex++] = action;
      }
    }

    _nextTimeStale = true;
    _count = writeIndex;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public long? GetNextActionTime()
  {
    if (_count == 0)
      return null;

    if (_nextTimeStale)
    {
      long nextTime = long.MaxValue;
      for (int i = 0; i < _count; i++)
      {
        ref var action = ref _actions[i];
        if (action.StartTime < nextTime)
        {
          nextTime = action.StartTime;
        }
      }
      _nextTime = nextTime;
    }

    return _nextTime;
  }
}

public class PerformanceTest
{
  private const int TotalIterations = 100_000;
  private const int WarmupIterations = 10_000;
  private const int BatchSize = 500;
  private static Random _random = new Random(42); // Fixed seed for reproducibility
  private static readonly ActionType[] ActionTypes = { ActionType.Action1, ActionType.Action2, ActionType.Action3 };

  public static void RunTests()
  {
    Func<Scheduler>[] schedulers = {
    () => new HighPerformanceActionScheduler(),
    () => new RevisedActionScheduler(),
    () => new TimelessScheduler()
  };

    string[] benchmarks = {
    "Standard Benchmark",
    "Far-Future Events",
    "Near-Future Scheduling",
    "Random Mix of Events",
    "Expired Events",
    "Frequent GetNextActionTime",
    "Recurring Events"
  };

    foreach (var benchmark in benchmarks)
    {
      for (int i = 0; i < 3; i++)
      {

        foreach (var factory in schedulers)
        {
          if (!GC.TryStartNoGCRegion(1024 * 1024 * 10))
          {
            Console.WriteLine("Failed to start NoGC region");
            return;
          }

          long initialAllocatedBytes = GC.GetTotalAllocatedBytes(true);
          var scheduler = factory();
          Console.WriteLine($"Running [{scheduler.GetType().Name}] {benchmark}...");

          WarmUp(scheduler);

          RunBenchmark(scheduler, benchmark);
          long afterBenchmarkAllocatedBytes = GC.GetTotalAllocatedBytes(true);

          GC.EndNoGCRegion();

          long allocatedBytes = afterBenchmarkAllocatedBytes - initialAllocatedBytes;
          Console.WriteLine($"Memory allocated: {allocatedBytes / 1024} KB for {scheduler.GetType().Name}");
          Console.WriteLine();
        }


        GC.Collect();
        GC.WaitForPendingFinalizers();
      }

      Console.WriteLine();
    }
  }

  private static void WarmUp(Scheduler scheduler)
  {
    int actionTypeIndex = 0;
    for (int i = 0; i < WarmupIterations; i++)
    {
      scheduler.ScheduleAction(ActionTypes[actionTypeIndex], i, i + 5, 3);
      scheduler.GetActionsForTime(i, _ => { });
      scheduler.GetNextActionTime();
      actionTypeIndex = (actionTypeIndex + 1) % ActionTypes.Length;
    }
  }

  private static long RunBenchmark(Scheduler scheduler, string benchmarkName)
  {
    var stopwatch = new Stopwatch();
    stopwatch.Start();

    switch (benchmarkName)
    {
      case "Standard Benchmark":
        StandardBenchmark(scheduler);
        break;
      case "Far-Future Events":
        FarFutureEvents(scheduler);
        break;
      case "Near-Future Scheduling":
        NearFutureScheduling(scheduler);
        break;
      case "Random Mix of Events":
        RandomMixOfEvents(scheduler);
        break;
      case "Expired Events":
        ExpiredEvents(scheduler);
        break;
      case "Frequent GetNextActionTime":
        FrequentGetNextActionTime(scheduler);
        break;
      case "Recurring Events":
        RecurringEvents(scheduler);
        break;
    }

    stopwatch.Stop();

    Console.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds} ms");
    return GC.GetTotalAllocatedBytes(true);
  }

  private static void StandardBenchmark(Scheduler scheduler)
  {
    for (int i = 0; i < TotalIterations; i++)
    {
      scheduler.ScheduleAction(ActionType.Action1, i, i + 10, 5);
      scheduler.GetActionsForTime(i, _ => { });
      scheduler.GetNextActionTime();
    }
  }

  private static void FarFutureEvents(Scheduler scheduler)
  {
    for (int batch = 0; batch < TotalIterations / BatchSize; batch++)
    {
      for (int i = 0; i < BatchSize; i++)
      {
        long startTime = batch * BatchSize + i + 10000;
        scheduler.ScheduleAction(ActionType.Action1, startTime, startTime + 10, 1);
      }

      for (int i = 0; i < BatchSize; i++)
      {
        scheduler.GetActionsForTime(batch * BatchSize + i, _ => { });
      }
    }
  }

  private static void NearFutureScheduling(Scheduler scheduler)
  {
    for (int i = 0; i < TotalIterations; i++)
    {
      scheduler.ScheduleAction(ActionType.Action1, i + 5, i + 15, 1);
      scheduler.GetActionsForTime(i, _ => { });
    }
  }

  private static void RandomMixOfEvents(Scheduler scheduler)
  {
    for (int batch = 0; batch < TotalIterations / BatchSize; batch++)
    {
      for (int i = 0; i < BatchSize; i++)
      {
        long startTime = batch * BatchSize + _random.Next(1, 1000);
        scheduler.ScheduleAction(ActionType.Action1, startTime, startTime + 10, 1);
      }

      for (int i = 0; i < BatchSize; i++)
      {
        scheduler.GetActionsForTime(batch * BatchSize + i, _ => { });
      }
    }
  }

  private static void ExpiredEvents(Scheduler scheduler)
  {
    for (int batch = 0; batch < TotalIterations / BatchSize; batch++)
    {
      for (int i = 0; i < BatchSize; i++)
      {
        long time = batch * BatchSize + i;
        scheduler.ScheduleAction(ActionType.Action1, time, time + 1, 1);
      }

      for (int i = 0; i < BatchSize; i++)
      {
        scheduler.GetActionsForTime(batch * BatchSize + i, _ => { });
      }
    }
  }

  private static void FrequentGetNextActionTime(Scheduler scheduler)
  {
    for (int i = 0; i < TotalIterations; i++)
    {
      if (i % 10 == 0)
      {
        scheduler.ScheduleAction(ActionType.Action1, i + 100, i + 110, 1);
      }
      scheduler.GetNextActionTime();
      scheduler.GetActionsForTime(i, _ => { });
    }
  }

  private static void RecurringEvents(Scheduler scheduler)
  {
    for (int batch = 0; batch < TotalIterations / BatchSize; batch++)
    {
      for (int i = 0; i < BatchSize / 100; i++) // Schedule 5 recurring events per batch
      {
        long startTime = batch * BatchSize + i * 100;
        scheduler.ScheduleAction(ActionType.Action1, startTime, startTime + 1000, 100);
      }

      for (int i = 0; i < BatchSize; i++)
      {
        scheduler.GetActionsForTime(batch * BatchSize + i, _ => { });
      }
    }
  }
}

class Program
{
  static void Main()
  {
    PerformanceTest.RunTests();
  }
}