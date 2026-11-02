using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using SteeLog.Datos;

class ConfigurationTests
{
    static void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS " + name); }
    static void Collect() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
    static int Handles() { var p = Process.GetCurrentProcess(); p.Refresh(); return p.HandleCount; }
    static void Main(string[] args)
    {
        string mode = args[0];
        if (mode == "override" || mode == "fallback")
        {
            var obj = new ConexionDB();
            var value = (string)typeof(ConexionDB).GetField("_connectionString", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
            Check(mode == "override" ? value.Contains("override_test") : !value.Contains("override_test") && value.Length > 0, mode);
        }
        else if (mode == "leak")
        {
            for (int i = 0; i < 10; i++) new ConexionDB();
            Collect(); int before = Handles();
            for (int i = 0; i < 500; i++) new ConexionDB();
            Collect(); int after = Handles();
            Console.WriteLine("Constructors: 500, handles before=" + before + ", after=" + after);
            Check(after - before <= 20, "configuration handles (allow 20 runtime handles, not 500 watchers)");
        }
        else if (mode == "failure")
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                int port = ((IPEndPoint)socket.LocalEndPoint).Port;
                System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"), "{\"ConnectionStrings\":{\"DefaultConnection\":\"Host=127.0.0.1;Port=" + port + ";Database=test;Username=test;Password=test;Timeout=1;Pooling=false\"}}");
                for (int i = 0; i < 3; i++) TryOpen();
                Collect(); int before = Handles();
                for (int i = 0; i < 30; i++) TryOpen();
                Collect(); int after = Handles();
                Console.WriteLine("Failed opens: 30, handles before=" + before + ", after=" + after);
                Check(after - before <= 10, "failed-open handles");
            }
        }
        else
        {
            try { new ConexionDB(); throw new Exception("Expected configuration failure"); }
            catch (InvalidOperationException e)
            {
                Check(e.Message.Contains("appsettings.json") && e.Message.Contains("PostgreSQL") && !e.ToString().Contains("secret_marker"), mode + " contextual and sanitized");
            }
        }
    }
    static void TryOpen()
    {
        try { new ConexionDB().GetConnection(); throw new Exception("Expected connection failure"); }
        catch (InvalidOperationException e) { if (e.InnerException == null || !e.Message.Contains("PostgreSQL")) throw; }
    }
}
