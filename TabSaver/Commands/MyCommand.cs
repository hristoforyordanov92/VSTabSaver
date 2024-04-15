using System.Reflection;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

namespace TabSaver
{
    [Command(PackageIds.SaveTabsCommand)]
    internal sealed class SaveTabsCommand : BaseCommand<SaveTabsCommand>
    {
        protected override void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DTE2 dte = (DTE2)ServiceProvider.GlobalProvider.GetService(typeof(DTE));

            Solution solution = new(dte.Solution.FullName);

            foreach (Document document in dte.Documents)
            {
                Window activeWindow = document.ActiveWindow;

                // some documents don't have a window. this happens with .xaml documents (maybe other types too).
                // they are explicitly closed from the X button on the tab, but they are still in the Documents collection
                if (activeWindow == null)
                    continue;

                bool isPinned = IsDocumentPinned(document);

                solution.Tabs.Add(new Tab(document.FullName, isPinned));
            }

            SaveFileManager.Save(solution);
        }

        /// <summary>
        /// Looks for a non-public property indicating if the document's window is pinned and returns the result.
        /// </summary>
        /// <param name="document">The document for which we want to know if it's pinned or not.</param>
        /// <returns>True if the document is pinned, otherwise false.</returns>
        private static bool IsDocumentPinned(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (document == null)
                return false;

            // some documents have multiple windows associated with them. idk why
            // if we use document.ActiveWindow then we might end up with "IsPinned = false", because
            // the DockViewElement is null for that specific window. so we go over each window
            // and we get a true/false based on wether at least a single one is true, otherwise false
            foreach (Window window in document.Windows)
            {
                if (window == null)
                    continue;

                PropertyInfo dockViewElementProperty = window
                    .GetType()
                    .GetProperty(
                        "DockViewElement",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty);

                if (dockViewElementProperty == null)
                    continue;

                object dockViewElementObject = dockViewElementProperty.GetValue(window);
                if (dockViewElementObject == null)
                    continue;

                if ((bool)dockViewElementObject.GetType().GetProperty("IsPinned").GetValue(dockViewElementObject))
                    return true;
            }

            return false;
        }
    }

    [Command(PackageIds.LoadTabsCommand)]
    internal sealed class LoadTabsCommand : BaseCommand<LoadTabsCommand>
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

                    dte.Documents.Open(tab.FullName);

                    if (tab.IsPinned)
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