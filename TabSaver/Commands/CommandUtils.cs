using System.Collections.Generic;
using System.Reflection;
using EnvDTE;

namespace TabSaver
{
    internal static class CommandUtils
    {
        public static IEnumerable<Tab> ExtractAllOpenTabs(EnvDTE.Documents documents, bool pinnedOnly = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (Document document in documents)
            {
                Window activeWindow = document.ActiveWindow;

                // some documents don't have a window. this happens with .xaml documents (maybe other types too).
                // even when explicitly closed from the X button on the tab, they are still in the Documents collection
                if (activeWindow == null)
                    continue;

                bool isPinned = IsDocumentPinned(document);

                if (pinnedOnly && !isPinned)
                    continue;

                yield return new Tab(document.FullName, isPinned);
            }
        }

        /// <summary>
        /// Looks for a non-public property indicating if the document's window is pinned and returns the result.
        /// </summary>
        /// <param name="document">The document for which we want to know if it's pinned or not.</param>
        /// <returns>True if the document is pinned, otherwise false.</returns>
        public static bool IsDocumentPinned(Document document)
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
}
