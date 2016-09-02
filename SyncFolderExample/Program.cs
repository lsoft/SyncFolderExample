using System;
using System.IO;
using System.Threading;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Files;

namespace SyncFolderExample
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            const string replica1RootPath = "_R0";
            const string replica2RootPath = "_R1";

            if (Directory.Exists(replica1RootPath))
            {
                Directory.Delete(replica1RootPath, true);
            }
            Directory.CreateDirectory(replica1RootPath);

            if (Directory.Exists(replica2RootPath))
            {
                Directory.Delete(replica2RootPath, true);
            }
            Directory.CreateDirectory(replica2RootPath);

            try
            {
                // Set options for the synchronization operation
                const FileSyncOptions options = 
                    FileSyncOptions.ExplicitDetectChanges |
                    FileSyncOptions.RecycleDeletedFiles |
                    FileSyncOptions.RecyclePreviousFileOnUpdates |
                    FileSyncOptions.RecycleConflictLoserFiles;

                var filter = new FileSyncScopeFilter();
                //filter.FileNameExcludes.Add("*.lnk"); // Exclude all *.lnk files

                Guid replica1Guid = Guid.Parse("181517DE-B950-4e62-9582-56F01884288D");
                Guid replica2Guid = Guid.Parse("86C9D79E-679D-4d33-A051-AA4EEFF17E55");

                using(var provider0 = new FileSyncProvider(replica1Guid, replica1RootPath, filter, options))
                {
                    using (var provider1 = new FileSyncProvider(replica2Guid, replica2RootPath, filter, options))
                    //using (var provider1 = new TestProvider(replica2Guid, Path.Combine(replica2RootPath, "_replica.sdf")))
                    {
                        var agent = CreateAgent(
                            provider0,
                            provider1
                            );

                        while (true)
                        {
                            provider0.DetectChanges();
                            provider1.DetectChanges();

                            agent.Synchronize();

                            Thread.Sleep(25);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\nException from File Synchronization Provider:\n" + e);
            }
        }

        public static SyncOrchestrator CreateAgent(
            FileSyncProvider sourceProvider,
            FileSyncProvider destinationProvider
            )
        {
            sourceProvider.AppliedChange += OnAppliedChange;
            sourceProvider.SkippedChange += OnSkippedChange;

            destinationProvider.AppliedChange += OnAppliedChange;
            destinationProvider.SkippedChange +=  OnSkippedChange;

            var agent = new SyncOrchestrator();
            agent.LocalProvider = sourceProvider;
            agent.RemoteProvider = destinationProvider;
            agent.Direction =
                SyncDirectionOrder.DownloadAndUpload
                //SyncDirectionOrder.Upload // Sync source to destination
                ;

            Console.WriteLine(
                "Synchronizing changes between {0} <-> {1}",
                sourceProvider.RootDirectoryPath,
                destinationProvider.RootDirectoryPath
                );

            return
                agent;
        }

        public static void OnAppliedChange(object sender, AppliedChangeEventArgs args)
        {
            switch (args.ChangeType)
            {
                case ChangeType.Create:
                    Console.WriteLine("-- Applied CREATE for file " + args.NewFilePath);
                    break;
                case ChangeType.Delete:
                    Console.WriteLine("-- Applied DELETE for file " + args.OldFilePath);
                    break;
                case ChangeType.Update:
                    Console.WriteLine("-- Applied UPDATE for file " + args.OldFilePath);
                    break;
                case ChangeType.Rename:
                    Console.WriteLine("-- Applied RENAME for file " + args.OldFilePath +
                                      " as " + args.NewFilePath);
                    break;
            }
        }

        public static void OnSkippedChange(object sender, SkippedChangeEventArgs args)
        {
            Console.WriteLine("-- Skipped applying " + args.ChangeType.ToString().ToUpper()
                              + " for " + (!string.IsNullOrEmpty(args.CurrentFilePath) ?
                                  args.CurrentFilePath : args.NewFilePath) + " due to error");

            if (args.Exception != null)
            {
                Console.WriteLine("   [" + args.Exception.Message + "]");
            }
        }
    }
}