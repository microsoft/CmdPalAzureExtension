﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using AzureExtension.DataModel;
using Serilog;

namespace AzureExtension.Test;

public partial class TestHelpers
{
    private const string DataBaseFileName = "AzureExtension-Test.db";
    private const string LogFileName = "AzureExtension-{now}.dhlog";
    private static readonly TimeSpan _cleanupRetryWaitTime = TimeSpan.FromSeconds(10);

    public static void CleanupTempTestOptions(TestOptions options, TestContext context)
    {
        // We put DataStore and Log into the same path.
        var path = options.DataStoreOptions.DataStoreFolderPath;

        // Directory delete will fail if a file has the name of the directory, so to be
        // thorough, check for file delete first.
        try
        {
            if (File.Exists(path))
            {
                context?.WriteLine($"Cleanup: Deleting file {path}");
                File.Delete(path);
            }

            if (Directory.Exists(path))
            {
                context?.WriteLine($"Cleanup: Deleting folder {path}");
                Directory.Delete(path, true);
            }
        }
        catch (IOException)
        {
            // Log writing being asynchronous can sometimes lead to a test finishing before its
            // log is done writing. This was leading to random intermittent test failures due
            // to the log file being in use. If we encounter an IOException, wait a few seconds
            // and try again.
            Thread.Sleep(_cleanupRetryWaitTime);
            context?.WriteLine($"Cleanup: Retrying Deleting folder {path}");
            Directory.Delete(path, true);

            // If it fails a second time we are intentionally not catching it, as that would
            // indicate a test failure that wasn't just a race involving I/O writing.
        }
    }

    public static TestOptions SetupTempTestOptions(TestContext context)
    {
        // Since all test created locations are ultimately captured in the Options, we will use
        // the Options as truth for storing the test location data to keep all of the
        // test locations in one data object to simplify test variables we are tracking and
        // to be consistent in test setup/cleanup.
        var path = GetUniqueFolderPath("AzureET");
        var options = new TestOptions();
        options.LogFileFolderRoot = path;
        options.LogFileName = LogFileName;
        options.DataStoreOptions.DataStoreFileName = DataBaseFileName;
        options.DataStoreOptions.DataStoreFolderPath = path;
        options.DataStoreOptions.DataStoreSchema = new AzureCacheDataStoreSchema();

        context?.WriteLine($"Temp folder for test run is: {GetTempTestFolderPath(options)}");
        context?.WriteLine($"Temp DataStore file path for test run is: {GetDataStoreFilePath(options)}");
        context?.WriteLine($"Temp Log file path for test run is: {GetLogFilePath(options)}");
        return options;
    }

    public static string GetTempTestFolderPath(TestOptions options)
    {
        // For simplicity putting log and datastore in same root folder.
        return options.DataStoreOptions.DataStoreFolderPath;
    }

    public static string GetDataStoreFilePath(TestOptions options)
    {
        return Path.Combine(options.DataStoreOptions.DataStoreFolderPath, options.DataStoreOptions.DataStoreFileName);
    }

    public static string GetLogFilePath(TestOptions options)
    {
        return FileSystem.SubstituteOutputFilename(options.LogFileName, options.LogFileFolderPath);
    }

    public static void ConfigureTestLog(TestOptions options, TestContext context)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: GetLogFilePath(options),
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate: "[{Timestamp:yyyy/MM/dd HH:mm:ss.fff} {Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
            .WriteTo.TestContextSink(
                context: context,
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate: "[{Timestamp:yyyy/MM/dd HH:mm:ss.fff} {Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    public static void CloseTestLog()
    {
        Log.CloseAndFlush();
    }
}
