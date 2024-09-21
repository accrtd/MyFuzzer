using ExifFuzzerPlugin;

#pragma warning disable CA1303

Console.WriteLine("Hello in plugin testing ;P");

// Testing ExifFuzzerPlugin

var inputData = """
    {
        "TargetFileLocation": "TODO", // <-------- TODO!
        "TargetSampleDataLocation": "TODO" // <-------- TODO!
    }
    """;

static void func(string inputData)
{
    var exifFuzzer = new ExifFuzzer();
    exifFuzzer.LoadArgs(inputData);
    exifFuzzer.SetCacheDir("TODO");  // <-------- TODO!
    var count = 0;
    while (count < 20)
    {
        exifFuzzer.Execute();
        ++count;
    }
}

var numberOfThreads = 30;
var threadArray = new Thread[numberOfThreads];
for (int i = 0; i < numberOfThreads; i++)
    threadArray[i] = new Thread(() => func(inputData));
for (int i = 0; i < numberOfThreads; i++)
    threadArray[i].Start();
for (int i = 0; i < numberOfThreads; i++)
    threadArray[i].Join();

Console.WriteLine("Finished ;P");