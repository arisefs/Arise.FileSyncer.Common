using System;
using System.IO;
using System.Linq;
using Arise.FileSyncer;
using Arise.FileSyncer.Common;

namespace Benchmark
{
    internal class Program
    {
        static void Main()
        {
            Log.SetLogger(new BenchLogger());

            Console.WriteLine("Arise FileSyncer Benchmark");

            var root = Directory.GetParent(Environment.GetCommandLineArgs()[0])!.FullName;
            Config.GetConfigFolderPath = () => root;

            var peerSource = new BenchPeer(0);
            var peerTarget = new BenchPeer(1);

            peerSource.ProgressTracker.ProgressUpdate += ProgressUpdate;
            peerSource.SendDiscoveryMessage();

            Console.ReadKey(true);
        }

        private static void ProgressUpdate(object? sender, ProgressUpdateEventArgs e)
        {
            if (e.Progresses.Count == 1)
            {
                var progress = e.Progresses.ElementAt(0);
                var speed = progress.Speed / 1_000_000;
                var current = progress.Current / 1_000_000;
                var maximum = progress.Maximum / 1_000_000;
                Console.WriteLine($"Current speed: {speed:##0.000} MB/s | {current} MB / {maximum} MB");
            }
        }
    }

    class BenchLogger : Logger
    {
        public override void Log(LogLevel level, string message)
        {
            if (level < LogLevel.Info)
            {
                base.Log(level, message);
            }
        }
    }
}
