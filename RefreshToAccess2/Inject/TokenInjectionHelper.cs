using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace RefreshToAccess2.Inject
{
    internal class TokenInjectionHelper
    {
        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);


        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
            byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess,
            IntPtr lpThreadAttributes, uint dwStackSize,
            IntPtr lpStartAddress, IntPtr lpParameter,
            uint dwCreationFlags, IntPtr lpThreadId);
        public static readonly ConcurrentDictionary<int, int> pidPortMap = new();
        public static List<int> injectedPids = new List<int>();
        public static async Task SendSwapToken(int port, string accessToken)
        {
            try
            {
                using var client = new HttpClient();
                var payload = new { access_token = accessToken };
                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    writer.WriteString("access_token", accessToken);
                    writer.WriteEndObject();
                }

                var content = new StringContent(Encoding.UTF8.GetString(stream.ToArray()), Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"http://localhost:{port}/token/swap", content);
                string body = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                bool success = root.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
                string message = root.TryGetProperty("message", out var messageProp) ? messageProp.GetString() : "(no message)";

                MessageBox.Show($"Token successfully injected.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR\n{ex.Message}");
            }
        }

        public static async Task HandleRequest(HttpListenerContext context)
        {
            if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/client/online")
            {
                try
                {
                    using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                    string json = await reader.ReadToEndAsync();

                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("pid", out var pidElement) || !pidElement.TryGetInt32(out int pid))
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Missing or invalid 'pid'"));
                        context.Response.Close();
                        return;
                    }

                    int port = -1;
                    if (root.TryGetProperty("port", out var portElement) && portElement.TryGetInt32(out int parsedPort))
                    {
                        port = parsedPort;
                    }

                    string error = null;
                    if (root.TryGetProperty("error", out var errorElement) && errorElement.ValueKind == JsonValueKind.String)
                    {
                        error = errorElement.GetString();
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        MessageBox.Show($"{error}");
                    }
                    else if (port > 0)
                    {
                        bool isFirstTime = !pidPortMap.ContainsKey(pid);
                        pidPortMap[pid] = port;

                        if (isFirstTime)
                        {
                            await TryInitHandshake(port);
                        }
                    }

                    context.Response.StatusCode = 200;
                    await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("OK"));
                    context.Response.Close();
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Error: " + ex.Message));
                    context.Response.Close();
                }
            }
            else
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
            }
        }
        static async Task TryInitHandshake(int port)
        {
            try
            {
                using var client = new HttpClient();
                var content = new StringContent("{}", Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"http://localhost:{port}/handshake/init", content);
                string body = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                bool success = root.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
                string message = root.TryGetProperty("message", out var messageProp) ? messageProp.GetString() : "(no message)";

                MessageBox.Show($"Found injected minecraft process");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[ERROR] Port {port}: {ex.Message}");
            }
        }
        public static bool InjectDll(int pid, string dllPath)
        {
            if (!System.IO.File.Exists(dllPath))
                return false;

            IntPtr hProcess = OpenProcess(0x001F0FFF, false, pid);
            if (hProcess == IntPtr.Zero) return false;

            IntPtr addr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            if (addr == IntPtr.Zero) return false;

            IntPtr allocMem = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)(dllPath.Length + 1),
                                             0x1000 | 0x2000, 0x04); // MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE
            if (allocMem == IntPtr.Zero) return false;

            WriteProcessMemory(hProcess, allocMem, Encoding.Default.GetBytes(dllPath), dllPath.Length, out _);

            IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, addr, allocMem, 0, IntPtr.Zero);

            return hThread != IntPtr.Zero;
        }
    }
}
