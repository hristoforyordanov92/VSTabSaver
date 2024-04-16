using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace TabSaver
{
    internal class Tab(string fullName, bool isPinned)
    {
        public string FullName { get; set; } = fullName;
        public bool IsPinned { get; set; } = isPinned;
    }

    internal class Solution(string fullName)
    {
        public string FullName { get; set; } = fullName;
        public List<Tab> Tabs { get; set; } = [];
    }

    internal class SolutionsSettings
    {
        public List<Solution> Solutions { get; set; } = [];
    }

    internal static class SaveFileManager
    {
        private static string ExtensionFolder =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TabSaverExtension");

        private static string SolutionsSettingsFilePath =>
            Path.Combine(ExtensionFolder, "SolutionsSettings.txt");

        private static void SaveSolutionsSettingsFile(SolutionsSettings solutionsSettings)
        {
            string serializedSaveFile = JsonConvert.SerializeObject(solutionsSettings, Formatting.Indented);
            File.WriteAllText(SolutionsSettingsFilePath, serializedSaveFile);
        }

        private static SolutionsSettings GetSavedSolutionsSettings()
        {
            if (File.Exists(SolutionsSettingsFilePath))
            {
                string loadedSolutionsSettings = File.ReadAllText(SolutionsSettingsFilePath);
                return JsonConvert.DeserializeObject<SolutionsSettings>(loadedSolutionsSettings);
            }
            else
            {
                SolutionsSettings newSettings = new();
                SaveSolutionsSettingsFile(newSettings);
                return newSettings;
            }
        }

        public static Solution GetSavedSolution(string solutionName)
        {
            if (string.IsNullOrWhiteSpace(solutionName))
                return null;

            SolutionsSettings currentSettings = GetSavedSolutionsSettings();

            return currentSettings.Solutions.FirstOrDefault(s =>
                string.Equals(s.FullName, solutionName, StringComparison.OrdinalIgnoreCase));
        }

        public static void SaveSolution(Solution solution)
        {
            Directory.CreateDirectory(ExtensionFolder);

            if (solution == null)
                return;

            SolutionsSettings currentSettings = GetSavedSolutionsSettings();

            int savedSolutionIndex =
                currentSettings.Solutions.FindIndex(s =>
                    string.Equals(s.FullName, solution.FullName, StringComparison.OrdinalIgnoreCase));

            if (savedSolutionIndex != -1)
            {
                currentSettings.Solutions[savedSolutionIndex] = solution;
            }
            else
            {
                currentSettings.Solutions.Add(solution);
            }

            SaveSolutionsSettingsFile(currentSettings);
        }
    }
}
