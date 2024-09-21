using ExifFuzzerPlugin.Models;
using PluginBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace ExifFuzzerPlugin;
#pragma warning disable CA1303

/// <summary>
/// Fuzzer to test https://github.com/mkttanabe/exif.
/// </summary>
public class ExifFuzzer : IFuzzerPlugin
{
#nullable enable
    private InputArgsModel? _args;
    private string? _cacheDirPath;
#nullable disable
    private int _iterationCounter;
    private byte[] _originalData;
    public string Name { get => "ExifFuzzer"; }
    public string Description { get => "Exif fuzzer plugin to test: https://github.com/mkttanabe/exif"; }

    public ExifFuzzer()
    {
        _iterationCounter = 0;
    }

    /// <summary>
    /// Parse plugn's arguments.
    /// </summary>
    /// <param name="args"></param>
    public void LoadArgs(string args) => _args = JsonSerializer.Deserialize<InputArgsModel>(args);

    /// <summary>
    /// Save path to cache directory.
    /// </summary>
    /// <param name="path"></param>
    public void SetCacheDir(string path) => _cacheDirPath = path;

    /// <summary>
    /// Testing method.
    /// </summary>
    /// <returns></returns>
    public int Execute()
    {
        if (_iterationCounter == 0)
        {
            var status = SetUp();
            if (status != 0)
                return status;
        }

        var modifiedData = BitFlipping();
        var newFile = Path.Combine(_cacheDirPath, $"{Path.GetFileName(_args.TargetSampleDataLocation)}_modification_{Guid.NewGuid()}");
        File.WriteAllBytes(newFile, modifiedData);

        var command = $"{_args.TargetFileLocation} {newFile}";
        var procStartInfo = new ProcessStartInfo
        {
            FileName = "cmd",
            Arguments = $"/c {command}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (var process = new Process())
        {
            process.StartInfo = procStartInfo;
            process.Start();

            // Add this: wait until process does its work
            if (!process.WaitForExit(3000)) // NOTE: 3 sec to wait
            {
                Console.WriteLine($"[ERROR-PLUGIN] {_iterationCounter} exif hanged on processing {newFile}");
                process.Kill(true);
            }
            else
            {
                // and only then read the result
                var exifErrMsg = process.StandardError.ReadToEnd();
                var exitCode = process.ExitCode;
                var isFailedTest = false;
                if (!string.IsNullOrWhiteSpace(exifErrMsg))
                {
                    Console.WriteLine($"[INFO-PLUGIN] {_iterationCounter} exif failed on processing {newFile} with message:");
                    Console.WriteLine(exifErrMsg);
                    isFailedTest = true;
                }
                if (exitCode < 0)
                {
                    Console.WriteLine($"[INFO-PLUGIN] {_iterationCounter} exif failed on processing {newFile} with returning code: {exitCode}");
                    isFailedTest = true;
                }
                if (!isFailedTest) // Remove the file that works
                    File.Delete(newFile);
            }
        }

        _iterationCounter += 1;
        return 0;
    }

    /// <summary>
    /// Configure init state.
    /// </summary>
    /// <returns></returns>
    private int SetUp()
    {
        if (string.IsNullOrWhiteSpace(_cacheDirPath))
        {
            Console.WriteLine("[ERROR-PLUGIN] In this fuzzer cache directory will be needed!");
            return -1;
        }
        else
        {
            _originalData = File.ReadAllBytes(_args.TargetSampleDataLocation);
            //Console.WriteLine(Convert.ToString(_originalData[0], 2));
            return 0;
        }
    }

    /// <summary>
    /// Randomly flip a bit.
    /// </summary>
    /// <returns></returns>
    private byte[] BitFlipping()
    {
        // We want to only flip 1% of the bytes we have and to not modify the SOI and EOI markers.
        var modifiedFile = new byte[_originalData.Length];
        Buffer.BlockCopy(_originalData, 0, modifiedFile, 0, modifiedFile.Length);
        var numOfFlips = Convert.ToInt64((_originalData.Length - 4) * 0.01);
        var indexes = Enumerable.Range(4, _originalData.Length - 4).ToList();

        for (var i = 0; i < numOfFlips; i++)
        {
            var idx = indexes.ElementAt(RandomNumberGenerator.GetInt32(0, indexes.Count));
            modifiedFile[idx] = BitFlip(_originalData[idx]);
        }
        return modifiedFile.ToArray();
    }

    /// <summary>
    /// Flip byte.
    /// </summary>
    /// <param name="byteToFlip"></param>
    /// <returns></returns>
    private static byte BitFlip(byte byteToFlip)
    {
        var currentByteStr = Convert.ToString(byteToFlip, 2);
        var listOfBytes = new List<System.Int16>();
        var indexToModify = RandomNumberGenerator.GetInt32(0, 8);

        if (currentByteStr.Length < 8)
            currentByteStr = currentByteStr.PadLeft(8, '0');
        foreach (var b in currentByteStr.ToList())
            listOfBytes.Add(Convert.ToInt16(char.GetNumericValue(b)));
        if (listOfBytes[indexToModify] == 1)
            listOfBytes[indexToModify] = 0;
        else
            listOfBytes[indexToModify] = 1;

        return Convert.ToByte(string.Join("", listOfBytes.ToArray()), 2);
    }
}
