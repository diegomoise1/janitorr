using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JanitorAspNet.Models;
using Microsoft.Extensions.Logging;

namespace JanitorAspNet.Services
{
    public interface IFileSystemService
    {
        Task<bool> ValidateSeedingAsync(LibraryItem item);
        Task CreateSymbolicLinksAsync(List<LibraryItem> items, string targetDirectory, LibraryType libraryType);
        Task CleanupDirectoryAsync(string directory);
        double GetDiskSpacePercentage(string path);
        Task<long> GetDirectorySizeAsync(string path);
        bool FileExists(string path);
        Task<bool> IsDirectoryEmptyAsync(string path);
        Task DeleteFileAsync(string path);
        Task DeleteDirectoryAsync(string path, bool recursive = false);
    }

    public class FileSystemService : IFileSystemService
    {
        private readonly ILogger<FileSystemService> _logger;

        public FileSystemService(ILogger<FileSystemService> logger)
        {
            _logger = logger;
        }

        public Task<bool> ValidateSeedingAsync(LibraryItem item)
        {
            if (string.IsNullOrEmpty(item.FilePath))
            {
                return Task.FromResult(false);
            }

            try
            {
                // Check if the file/directory exists and is being actively used
                // This is a simplified check - in reality, you'd want to check:
                // 1. If it's a torrent file being seeded
                // 2. If the file is currently being accessed
                // 3. If there are active network connections to the file

                if (Directory.Exists(item.FilePath))
                {
                    var directory = new DirectoryInfo(item.FilePath);
                    
                    // Check for torrent-related files that might indicate seeding
                    var torrentFiles = directory.GetFiles("*.torrent", SearchOption.AllDirectories);
                    if (torrentFiles.Any())
                    {
                        _logger.LogInformation("Found torrent files for {Path}, assuming seeding", item.FilePath);
                        return Task.FromResult(true);
                    }

                    // Check for recent file access (within last hour)
                    var recentFiles = directory.GetFiles("*", SearchOption.AllDirectories)
                        .Where(f => f.LastAccessTime > DateTime.Now.AddHours(-1));
                    
                    if (recentFiles.Any())
                    {
                        _logger.LogInformation("Found recently accessed files for {Path}, assuming seeding", item.FilePath);
                        return Task.FromResult(true);
                    }
                }
                else if (File.Exists(item.FilePath))
                {
                    var file = new FileInfo(item.FilePath);
                    
                    // Check if file was accessed recently
                    if (file.LastAccessTime > DateTime.Now.AddHours(-1))
                    {
                        _logger.LogInformation("File {Path} was recently accessed, assuming seeding", item.FilePath);
                        return Task.FromResult(true);
                    }
                }

                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating seeding status for {Path}", item.FilePath);
                return Task.FromResult(true); // Err on the side of caution
            }
        }

        public async Task CreateSymbolicLinksAsync(List<LibraryItem> items, string targetDirectory, LibraryType libraryType)
        {
            _logger.LogInformation("Creating symbolic links for {Count} items in {TargetDirectory}", items.Count, targetDirectory);

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
                _logger.LogInformation("Created target directory: {Directory}", targetDirectory);
            }

            foreach (var item in items)
            {
                try
                {
                    if (string.IsNullOrEmpty(item.FilePath) || !Directory.Exists(item.FilePath))
                    {
                        _logger.LogWarning("Source path does not exist for item: {Title}", item.Title);
                        continue;
                    }

                    var linkName = SanitizeFileName(item.Title);
                    var linkPath = Path.Combine(targetDirectory, linkName);

                    // Avoid duplicate links
                    if (Directory.Exists(linkPath) || File.Exists(linkPath))
                    {
                        _logger.LogInformation("Link already exists for: {Title}", item.Title);
                        continue;
                    }

                    // Create symbolic link using Process.Start with ln command on Unix systems
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        var processInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "ln",
                            Arguments = $"-s \"{item.FilePath}\" \"{linkPath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };

                        using var process = System.Diagnostics.Process.Start(processInfo);
                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                            if (process.ExitCode == 0)
                            {
                                _logger.LogInformation("Created symbolic link: {LinkPath} -> {SourcePath}", linkPath, item.FilePath);
                            }
                            else
                            {
                                var error = await process.StandardError.ReadToEndAsync();
                                _logger.LogError("Failed to create symbolic link: {Error}", error);
                            }
                        }
                    }
                    else
                    {
                        // On Windows, create junction or directory symlink
                        _logger.LogWarning("Symbolic link creation not implemented for Windows platform");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating symbolic link for {Title}", item.Title);
                }
            }
        }

        public async Task CleanupDirectoryAsync(string directory)
        {
            _logger.LogInformation("Cleaning up directory: {Directory}", directory);

            try
            {
                if (!Directory.Exists(directory))
                {
                    _logger.LogInformation("Directory does not exist: {Directory}", directory);
                    return;
                }

                var directoryInfo = new DirectoryInfo(directory);
                
                // Remove empty subdirectories
                foreach (var subDir in directoryInfo.GetDirectories())
                {
                    await CleanupEmptyDirectoriesRecursiveAsync(subDir.FullName);
                }

                // Remove the directory itself if it's empty
                if (await IsDirectoryEmptyAsync(directory))
                {
                    Directory.Delete(directory);
                    _logger.LogInformation("Removed empty directory: {Directory}", directory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up directory: {Directory}", directory);
            }
        }

        public double GetDiskSpacePercentage(string path)
        {
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(path) ?? "/");
                var usedSpace = drive.TotalSize - drive.AvailableFreeSpace;
                var percentage = (double)usedSpace / drive.TotalSize * 100;
                
                _logger.LogDebug("Disk space for {Path}: {Percentage:F2}% used", path, percentage);
                return percentage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting disk space for path: {Path}", path);
                return 0.0;
            }
        }

        public async Task<long> GetDirectorySizeAsync(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return 0;
                }

                var directoryInfo = new DirectoryInfo(path);
                long size = 0;

                // Calculate size of all files in directory and subdirectories
                await Task.Run(() =>
                {
                    size = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                        .Sum(file => file.Length);
                });

                return size;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating directory size for: {Path}", path);
                return 0;
            }
        }

        public bool FileExists(string path) => File.Exists(path);

        public async Task<bool> IsDirectoryEmptyAsync(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return true;
                }

                return await Task.Run(() => !Directory.EnumerateFileSystemEntries(path).Any());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if directory is empty: {Path}", path);
                return false;
            }
        }

        public async Task DeleteFileAsync(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    await Task.Run(() => File.Delete(path));
                    _logger.LogInformation("Deleted file: {Path}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {Path}", path);
                throw;
            }
        }

        public async Task DeleteDirectoryAsync(string path, bool recursive = false)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    await Task.Run(() => Directory.Delete(path, recursive));
                    _logger.LogInformation("Deleted directory: {Path}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting directory: {Path}", path);
                throw;
            }
        }

        private async Task CleanupEmptyDirectoriesRecursiveAsync(string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    return;
                }

                var directoryInfo = new DirectoryInfo(directory);
                
                // First, clean up subdirectories
                foreach (var subDir in directoryInfo.GetDirectories())
                {
                    await CleanupEmptyDirectoriesRecursiveAsync(subDir.FullName);
                }

                // Then check if this directory is empty and remove it
                if (await IsDirectoryEmptyAsync(directory))
                {
                    Directory.Delete(directory);
                    _logger.LogInformation("Removed empty directory: {Directory}", directory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during recursive directory cleanup: {Directory}", directory);
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Length > 255 ? sanitized.Substring(0, 255) : sanitized;
        }
    }
}
