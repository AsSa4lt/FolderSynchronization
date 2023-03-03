using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

class FolderSynchronizer
{
    private static string sourceFolder;
    private static string replicaFolder;
    private static string logFilePath;
    private static int synchronizationInterval;
    private static FileSystemWatcher watcher;
    private static bool isSyncing;

    static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: FolderSynchronizer.exe sourceFolder replicaFolder synchronizationIntervalInSeconds logFilePath");
            return;
        }

        sourceFolder = args[0]; 
        replicaFolder = args[1];
        synchronizationInterval = int.Parse(args[2]);
        logFilePath = args[3];
        /*sourceFolder = "/Users/rostyslav/Desktop/1"; 
        replicaFolder = "/Users/rostyslav/Desktop/2";
        synchronizationInterval = 5;
        logFilePath = "/Users/rostyslav/Desktop/logs.txt";*/

        watcher = new FileSystemWatcher(sourceFolder);
        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
        watcher.IncludeSubdirectories = true;
        watcher.Changed += OnChanged;
        watcher.Created += OnChanged;
        watcher.Deleted += OnChanged;
        watcher.Renamed += OnRenamed;
        watcher.EnableRaisingEvents = true;

        Console.WriteLine("FolderSynchronizer started");
        Console.WriteLine("Source folder: {0}", sourceFolder);
        Console.WriteLine("Replica folder: {0}", replicaFolder);
        Console.WriteLine("Synchronization interval: {0} seconds", synchronizationInterval);
        Console.WriteLine("Log file: {0}", logFilePath);

        SyncFolders(); // initial synchronization

        Timer timer = new Timer(SyncFolders, null, synchronizationInterval * 1000, synchronizationInterval * 1000);

        Console.ReadLine();
    }

    private static void OnChanged(object source, FileSystemEventArgs e)
    {
        LogEvent(e.ChangeType.ToString(), e.FullPath);
    }

    private static void OnRenamed(object source, RenamedEventArgs e)
    {
        LogEvent("Renamed", e.OldFullPath + " -> " + e.FullPath);
    }

    private static void LogEvent(string action, string filePath)
    {
        using (StreamWriter writer = new StreamWriter(logFilePath, true))
        {
            writer.WriteLine("{0} {1} {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), action, filePath);
        }

        Console.WriteLine("{0} {1} {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), action, filePath);
    }

    private static void SyncFolders(object state = null)
    {
        if (isSyncing) return;
        isSyncing = true;

        try
        {
            Console.WriteLine("Synchronizing folders...");

            // Copy files from source folder to replica folder and delete
            CopyNewOrChangedFiles(sourceFolder, replicaFolder);

            Console.WriteLine("Folders synchronized");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error synchronizing folders: {0}", ex.Message);
        }

        isSyncing = false;
    }

    private static void CopyNewOrChangedFiles(string source, string replica)
    {
        foreach (string file in Directory.GetFiles(source))
        {
            string replicaFile = Path.Combine(replica, Path.GetFileName(file));
            bool isFileNewOrChanged = !File.Exists(replicaFile) || GetMD5Hash(file) != GetMD5Hash(replicaFile);
            if (isFileNewOrChanged)
            {
                if (File.Exists(replicaFile))
                {
                    File.Delete(replicaFile);
                    LogEvent("Deleted", replicaFile);
                }

                File.Copy(file, replicaFile);
                LogEvent("Copied", file);
            }
        }

        foreach (string file in Directory.GetFiles(replica))
        {
            string sourceFile = Path.Combine(source, Path.GetFileName(file));
            bool isFileDeleted = !File.Exists(sourceFile);
            if (isFileDeleted)
            {
                File.Delete(file);
                LogEvent("Deleted", file);
            }
        }

        foreach (string dir in Directory.GetDirectories(source))
        {
            string replicaDir = Path.Combine(replica, Path.GetFileName(dir));
            if (!Directory.Exists(replicaDir))
            {
                Directory.CreateDirectory(replicaDir);
                LogEvent("Created", replicaDir);
            }

            CopyNewOrChangedFiles(dir, replicaDir);
        }
    }



    private static string GetMD5Hash(string filePath)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
            }
        }
    }
}

