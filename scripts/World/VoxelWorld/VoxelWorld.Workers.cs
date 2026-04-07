using System;
using System.Threading;
using System.Threading.Tasks;
using justonlytnt.World.Jobs;

namespace justonlytnt.World;

public sealed partial class VoxelWorld
{
    private void StartWorkers()
    {
        _workerCts = new CancellationTokenSource();
        int count = Godot.Mathf.Max(1, Config.WorkerCount);

        for (int i = 0; i < count; i++)
        {
            _workers.Add(Task.Run(() => WorkerLoop(_workerCts.Token), _workerCts.Token));
        }
    }

    private void StopWorkers()
    {
        if (_workerCts is null)
        {
            return;
        }

        _workerCts.Cancel();
        _jobSignal.Set();

        try
        {
            Task.WaitAll(_workers.ToArray(), 1000);
        }
        catch (Exception)
        {
            // Ignore cancellation races on shutdown.
        }

        _workers.Clear();
        _workerCts.Dispose();
        _workerCts = null;
    }

    private void WorkerLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            IChunkJob? job = null;

            lock (_jobLock)
            {
                if (_jobQueue.Count > 0)
                {
                    job = _jobQueue.Dequeue();
                }
            }

            if (job is null)
            {
                _jobSignal.WaitOne(8);
                continue;
            }

            IChunkJobResult result = job.Execute();
            _resultQueue.Enqueue(result);
        }
    }

    private void ScheduleJob(IChunkJob job)
    {
        lock (_jobLock)
        {
            _jobQueue.Enqueue(job, job.Priority);
        }

        _jobSignal.Set();
    }
}
