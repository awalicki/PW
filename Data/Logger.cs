// Logger.cs (nowy plik)
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TP.ConcurrentProgramming.Data
{
    public class Logger : IDisposable
    {
        private readonly DiagnosticBuffer _buffer;
        private readonly string _logFilePath;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task? _loggingTask;
        private readonly object _lock = new object();
        private bool _disposed = false;

        public Logger(string logFilePath, int bufferCapacity = 1000)
        {
            _logFilePath = logFilePath;
            _buffer = new DiagnosticBuffer(bufferCapacity);
            _cancellationTokenSource = new CancellationTokenSource();
            _loggingTask = Task.Run(() => ProcessLogQueue(_cancellationTokenSource.Token));
        }

        public void Log(DiagnosticData data)
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    Console.WriteLine("Logger is disposed. Cannot log data.");
                    return;
                }

                if (!_buffer.TryAdd(data))
                {
                    Console.WriteLine("Diagnostic buffer is full. Dropping data.");
                }
            }
        }

        private async Task ProcessLogQueue(CancellationToken token)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(_logFilePath, append: true))
                {
                    while (!token.IsCancellationRequested)
                    {
                        DiagnosticData? dataToLog = null;
                        lock (_lock)
                        {
                            if (_buffer.TryTake(out dataToLog))
                            {
                                // Data found, proceed to log
                            }
                            else
                            {
                                // Buffer is empty, wait for a short period before checking again
                            }
                        }

                        if (dataToLog != null)
                        {
                            await sw.WriteLineAsync(dataToLog.ToJson());
                            await sw.FlushAsync(); // Ensure data is written to disk promptly
                        }
                        else
                        {
                            await Task.Delay(50, token); // Wait if buffer is empty
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Task was cancelled, expected behavior during shutdown
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in logging task: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Logging task finished.");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Console.WriteLine("Disposing Logger...");
                    _cancellationTokenSource.Cancel();
                    try
                    {
                        _loggingTask?.Wait(); // Wait for the logging task to finish
                    }
                    catch (AggregateException ae)
                    {
                        foreach (var inner in ae.InnerExceptions)
                        {
                            if (inner is not TaskCanceledException)
                            {
                                Console.WriteLine($"Caught unexpected exception during Logger Dispose: {inner.GetType().Name}: {inner.Message}");
                            }
                        }
                    }
                    Console.WriteLine("Logger disposed.");
                }
                _disposed = true;
            }
        }
    }

    internal class DiagnosticBuffer
    {
        private readonly DiagnosticData?[] _buffer;
        private int _head;
        private int _tail;
        private int _count;
        private readonly int _capacity;
        private readonly object _bufferLock = new object();

        public DiagnosticBuffer(int capacity)
        {
            _capacity = capacity;
            _buffer = new DiagnosticData?[capacity];
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        public bool TryAdd(DiagnosticData item)
        {
            lock (_bufferLock)
            {
                if (_count == _capacity)
                {
                    return false; // Buffer is full
                }

                _buffer[_tail] = item;
                _tail = (_tail + 1) % _capacity;
                _count++;
                return true;
            }
        }

        public bool TryTake(out DiagnosticData? item)
        {
            lock (_bufferLock)
            {
                if (_count == 0)
                {
                    item = default;
                    return false; // Buffer is empty
                }

                item = _buffer[_head];
                _buffer[_head] = default; // Clear the slot
                _head = (_head + 1) % _capacity;
                _count--;
                return true;
            }
        }
    }
}