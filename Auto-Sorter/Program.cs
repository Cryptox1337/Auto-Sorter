using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileSorter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Auto-Sorter by xCrypto1337\n");

            // Set the directory paths
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var inputDirectory = Path.Combine(baseDirectory, "input");
            var outputDirectory = Path.Combine(baseDirectory, "output");

            // Create the input and output directories if they don't exist
            Directory.CreateDirectory(inputDirectory);
            Directory.CreateDirectory(outputDirectory);

            // Get all the files in the input directory and its subdirectories
            var allFilesInInputDirectory = Directory.GetFiles(inputDirectory, "*", SearchOption.AllDirectories);

            // Initialize the progress bar
            var totalFiles = 0;
            var progressLock = new object();
            Console.WriteLine($"Sorting {allFilesInInputDirectory.Length} files...\n");

            // Setup cancellation token
            using var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
            };

            try
            {
                await Task.Run(() => Parallel.ForEach(allFilesInInputDirectory, new ParallelOptions { CancellationToken = cancellationTokenSource.Token }, filePath =>
                {
                    // Get the file extension
                    var fileExtension = Path.GetExtension(filePath).ToLower();

                    // Skip files without extensions
                    if (string.IsNullOrEmpty(fileExtension))
                    {
                        return;
                    }

                    // Create a new subdirectory in the output directory with the name of the file extension
                    var outputSubdirectory = Path.Combine(outputDirectory, fileExtension.TrimStart('.'));
                    Directory.CreateDirectory(outputSubdirectory);

                    // Copy the file to the appropriate subdirectory in the output directory
                    var outputFilePath = Path.Combine(outputSubdirectory, Path.GetFileNameWithoutExtension(filePath) + fileExtension);

                    try
                    {
                        File.Copy(filePath, outputFilePath, overwrite: true);
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine($"Error copying file {filePath}: {e.Message}");
                    }

                    // Update the progress bar
                    lock (progressLock)
                    {
                        totalFiles++;
                        Console.Write($"\rProcessed {totalFiles} of {allFilesInInputDirectory.Length} files...");
                    }

                    // Check for cancellation request
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                }));
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nOperation cancelled.");
            }

            // Clear the progress bar
            Console.WriteLine("\nDone!");
            await Task.Delay(1000);
        }
    }
}