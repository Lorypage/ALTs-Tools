using System;
using System.Threading;
using System.Windows;

namespace RefreshToAccess2.Helpers
{
    /// <summary>
    /// Drop-in replacement for <see cref="System.Windows.Clipboard"/> that is both
    /// crash-safe and UI-freeze-safe.
    ///
    /// The Win32 clipboard can only be held by one process at a time, so clipboard
    /// managers, IMEs, browsers or remote-desktop agents occasionally cause
    /// <c>SetText</c>/<c>GetText</c> to throw <c>CLIPBRD_E_CANT_OPEN</c> (0x800401D0)
    /// — and the underlying OLE flush can *block the calling thread* for a long time.
    /// Running that on the UI thread is what makes the window go "Not Responding".
    ///
    /// This wrapper therefore runs every clipboard operation on a short-lived
    /// dedicated STA thread and waits for it with a bounded timeout, so the UI thread
    /// is never blocked for more than <see cref="OpTimeoutMs"/>. Failures are
    /// swallowed (set) or returned as empty (get) rather than surfaced as crashes.
    /// </summary>
    public static class SafeClipboard
    {
        private const int MaxAttempts = 6;
        private const int DelayMs     = 80;
        // Hard cap on how long the UI thread will wait for a clipboard op.
        private const int OpTimeoutMs = 1000;

        /// <summary>Best-effort copy. Never throws; returns false if the clipboard
        /// could not be opened within the timeout.</summary>
        public static bool TrySetText(string text)
        {
            string value = text ?? string.Empty;
            // copy:false avoids OleFlushClipboard, which can block for a long time
            // when another process holds the clipboard. The downside is the copied
            // text is cleared when this process exits — fine for copying tokens,
            // and worth it to keep the UI responsive.
            return RunSta(() => System.Windows.Clipboard.SetDataObject(value, false));
        }

        public static void SetText(string text) => TrySetText(text);

        public static string GetText()
        {
            string result = string.Empty;
            RunSta(() => result = System.Windows.Clipboard.GetText());
            return result;
        }

        public static bool ContainsText()
        {
            bool result = false;
            RunSta(() => result = System.Windows.Clipboard.ContainsText());
            return result;
        }

        public static void Clear()
            => RunSta(System.Windows.Clipboard.Clear);

        /// <summary>
        /// Runs <paramref name="action"/> on a dedicated STA thread with retry on
        /// transient clipboard locks, waiting at most <see cref="OpTimeoutMs"/> so the
        /// caller (usually the UI thread) never freezes. Returns true on success,
        /// false on timeout or after exhausting retries. Never rethrows.
        /// </summary>
        private static bool RunSta(Action action)
        {
            bool success = false;

            var worker = new Thread(() =>
            {
                for (int attempt = 1; attempt <= MaxAttempts; attempt++)
                {
                    try
                    {
                        action();
                        success = true;
                        return;
                    }
                    catch (Exception) when (attempt < MaxAttempts)
                    {
                        // Another process holds the clipboard; back off and retry.
                        Thread.Sleep(DelayMs);
                    }
                    catch (Exception)
                    {
                        // Exhausted retries — give up quietly.
                        return;
                    }
                }
            })
            {
                IsBackground = true,
                Name = "SafeClipboard"
            };

            worker.SetApartmentState(ApartmentState.STA);
            worker.Start();

            // Bounded wait: if the clipboard op blocks/contends past the timeout we
            // return rather than hang the UI. The background thread is free to finish
            // (or be abandoned) on its own.
            if (!worker.Join(OpTimeoutMs))
                return false;

            return success;
        }
    }
}
