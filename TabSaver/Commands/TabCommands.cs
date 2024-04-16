using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

namespace TabSaver
{
    [Command(PackageIds.SavePinnedTabsCommand)]
    internal sealed class SavePinnedTabsCommand : BaseCommand<SavePinnedTabsCommand>
    {
        protected override void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DTE2 dte = (DTE2)ServiceProvider.GlobalProvider.GetService(typeof(DTE));

            Solution solution = new(dte.Solution.FullName);

            solution.Tabs.AddRange(CommandUtils.ExtractAllOpenTabs(dte.Documents, true));

            SaveFileManager.SaveSolution(solution);
        }
    }

    [Command(PackageIds.SaveAllTabsCommand)]
    internal sealed class SaveAllTabsCommand : BaseCommand<SaveAllTabsCommand>
    {
        protected override void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DTE2 dte = (DTE2)ServiceProvider.GlobalProvider.GetService(typeof(DTE));

            Solution solution = new(dte.Solution.FullName);

            solution.Tabs.AddRange(CommandUtils.ExtractAllOpenTabs(dte.Documents));

            SaveFileManager.SaveSolution(solution);
        }
    }

    [Command(PackageIds.RestoreSavedTabsCommand)]
    internal sealed class RestoreSavedTabsCommand : BaseCommand<RestoreSavedTabsCommand>
    {
        protected override void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DTE2 dte = (DTE2)ServiceProvider.GlobalProvider.GetService(typeof(DTE));

            Solution solution = SaveFileManager.GetSavedSolution(dte.Solution.FullName);
            if (solution == null)
            {
                VS.MessageBox.Show(
                    "There are no tabs saved for this solution.",
                    buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK);

                return;
            }

            foreach (Tab tab in solution.Tabs)
            {
                try
                {
                    ProjectItem proj = dte.Solution.FindProjectItem(tab.FullName);
                    if (proj == null)
                        continue;

                    Document document = dte.Documents.Open(tab.FullName);

                    if (tab.IsPinned && !CommandUtils.IsDocumentPinned(document))
                        dte.ExecuteCommand("Window.PinTab");
                }
                catch (Exception)
                {
                    // failed documents
                }
            }
        }
    }
}