using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace ScrcpyController.Core
{
    public class ScrcpyValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> MissingFiles { get; set; } = new();
        public string ScrcpyPath { get; set; } = "";
    }

    public class ScrcpyValidator
    {
        private readonly string[] _requiredFiles = {
            "scrcpy.exe",
            "adb.exe",
            "AdbWinApi.dll",
            "AdbWinUsbApi.dll"
        };

        public ScrcpyValidationResult ValidateScrcpyPath(string path)
        {
            var result = new ScrcpyValidationResult
            {
                ScrcpyPath = path
            };

            // Check if path is empty or null
            if (string.IsNullOrWhiteSpace(path))
            {
                result.MissingFiles.AddRange(_requiredFiles);
                return result;
            }

            // Check if path exists
            if (!Directory.Exists(path))
            {
                result.MissingFiles.AddRange(_requiredFiles);
                return result;
            }

            // Check each required file
            foreach (string file in _requiredFiles)
            {
                string fullPath = Path.Combine(path, file);
                if (!File.Exists(fullPath))
                {
                    result.MissingFiles.Add(file);
                }
            }

            result.IsValid = result.MissingFiles.Count == 0;
            return result;
        }

        public ScrcpyValidationResult FindAndValidateScrcpyPath()
        {
            // Check common installation paths
            string[] commonPaths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "scrcpy"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "scrcpy"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scrcpy"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "scrcpy"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "scrcpy")
            };

            foreach (string path in commonPaths)
            {
                var result = ValidateScrcpyPath(path);
                if (result.IsValid)
                {
                    return result;
                }
            }

            // If not found in common paths, return invalid result
            return new ScrcpyValidationResult { IsValid = false, MissingFiles = _requiredFiles.ToList() };
        }
    }
}