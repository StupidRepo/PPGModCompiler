using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using ModModels;
using Newtonsoft.Json;
using WatsonTcp;

namespace PPGModCompiler
{
    public class Compiler
    {
        public static void RunFileCheck()
        {
            Console.WriteLine("Running quick check for some folders...");
            bool found = (Directory.Exists(pathlol + "workshop") || Directory.Exists(pathlol + "common"));
            while(!found)
            {
                Console.WriteLine("Couldn't find the 'workshop' folder or the 'common' folder for THE STEAM PATH! Try entering another path, or press CTRL+C to quit!");
                Console.Write("Path: ");
                pathlol = Console.ReadLine();
                found = (Directory.Exists(pathlol + "workshop") || Directory.Exists(pathlol + "common"));
            }
            StreamWriter lp = File.CreateText("last_path");
            lp.Write(pathlol);
            lp.Close();
            Console.WriteLine("Both folders found; continuing...");
        }

        [Obsolete]
        public static int Main(string[] args)
        {
            Console.WriteLine("Checking for 'last_path'...");
            if(File.Exists("last_path"))
            {
                Console.WriteLine("last_path found!");
                pathlol = File.ReadAllText("last_path");

                RunFileCheck();
            } else
            {
                Console.WriteLine("last_path not found.");
                Console.Write("Enter (or paste) the path to the Steam folder (with trailing slash!): ");
                pathlol = Console.ReadLine();

                RunFileCheck();
            }

            CompilerConfig config = ReadConfig();
            compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, false, null, null, null, null, OptimizationLevel.Release, false, false, null, null, default(ImmutableArray<byte>), null, Platform.AnyCpu, ReportDiagnostic.Default, 4, null, true, false, null, null, null, null, null, false, MetadataImportOptions.Public, NullableContextOptions.Disable);
            if (config.ShutdownWhenGameNotFound && args.Length == 1 && int.TryParse(args[0], out gameProcessId))
            {
                monitor = new Thread(new ParameterizedThreadStart(MonitorLoop));
                monitor.IsBackground = true;
                monitor.Start();
            }

            server = new WatsonTcpServer(config.Hostname, config.Port);

            WatsonTcpServerSettings settings = server.Settings;
            settings.Logger = (Action<Severity, string>)Delegate.Combine(settings.Logger, new Action<Severity, string>(delegate (Severity s, string m)
            {
                Console.WriteLine("[{0}] {1}", s, m);
            }));
            server.Events.ClientConnected += delegate (object o, ConnectionEventArgs e)
            {
                Console.WriteLine("Client {0} connected", e.Client.IpPort);
                server.SendAndWait(3000, e.Client.IpPort, "alles goed ouwe", null);
            };
            server.Events.ClientDisconnected += delegate (object o, DisconnectionEventArgs e)
            {
                Console.WriteLine("Client {0} disconnected", e.Client.IpPort);
            };
            server.Events.MessageReceived += delegate (object o, MessageReceivedEventArgs e)
            {
                Console.WriteLine("Received message from {0}", e.Client.IpPort);
                ProcessMessage(e.Data, e.Client.IpPort);
            };
            server.Events.ServerStopped += delegate (object o, EventArgs e)
            {
                Console.WriteLine("Server was stopped");
            };

            server.Callbacks.SyncRequestReceived = (SyncRequest e) => new SyncResponse(e, "deze snap ik niet ouwe");

            try
            {
                server.Start();
            }
            catch (System.Net.Sockets.SocketException)
            {
                Console.WriteLine("A server is already running, please quit it and try again. If PPG is open, please close it.");
                return 255;
            }

            Console.WriteLine(string.Format("Started listening on {0}:{1}", config.Hostname, config.Port));
            while (!server.IsListening)
            {
                Thread.Sleep(500);
            }
            while (server.IsListening)
            {
                Thread.Sleep(500);
                if (!shouldStayAlive)
                {
                    if (server.IsListening)
                    {
                        server.Stop();
                    }
                    return 0;
                }
            }
            return 0;
        }

        private static void MonitorLoop(object _)
        {
            for (; ; )
            {
                Thread.Sleep(1000);
                try
                {
                    if (Process.GetProcessById(gameProcessId).HasExited)
                    {
                        MonitorLoop();
                    }
                }
                catch (Exception)
                {
                    MonitorLoop();
                }
            }
        }

        private static CompilerConfig ReadConfig()
        {
            if (File.Exists("config.json"))
            {
                try
                {
                    return JsonConvert.DeserializeObject<CompilerConfig>(File.ReadAllText("config.json"));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to read compiler config: {0}", e);
                }
            }
            return new CompilerConfig();
        }

        private static void ProcessMessage(byte[] data, string remote)
        {
            while (isBusy)
            {
                Thread.Sleep(16);
            }
            isBusy = true;
            string text = Encoding.UTF8.GetString(data);
            try
            {
                if (text == "quit")
                {
                    Console.WriteLine("Quit received.");
                    server.Stop();
                    shouldStayAlive = false;
                    isBusy = false;
                }
                else
                {
                    //string mytext = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\People Playground\\People Playground_Data\\hello.txt";
                    text = text.Replace("C:\\\\Program Files (x86)\\\\Steam\\\\steamapps\\\\workshop", pathlol+"workshop").Replace("C:\\\\Program Files (x86)\\\\Steam\\\\steamapps\\\\common\\\\People Playground\\\\", pathlol+"common/People Playground/").Replace("\\\\", "/");
                    Console.WriteLine("Received mod compile instructions!", text);
                    CompileMod(JsonConvert.DeserializeObject<ModCompileInstructions>(text), remote);
                }
            }
            catch (Exception exception)
            {
                return;
            }
            finally
            {
                isBusy = false;
            }
        }

        private static void CompileMod(ModCompileInstructions instructions, string remote)
        {
            void Reply(CompilerReply reply)
            {
                reply.ID = instructions.ID;
                Task.Run(async delegate {
                    string data = JsonConvert.SerializeObject(reply);
                    try
                    {
                        await server.SendAsync(remote, data);
                        Console.WriteLine("Sent {0}: {1} to {2}", reply.State, reply.Message, remote);
                    }
                    catch (Exception arg)
                    {
                        Console.Error.WriteLine("Failed to send reply to {1}: {0}", arg, remote);
                    }
                });
            }
            List<string> sources = new List<string>();
            File.WriteAllText("last_instructions", JsonConvert.SerializeObject(instructions, Formatting.Indented));
            if (!string.IsNullOrWhiteSpace(instructions.InsertSourceB64))
            {
                sources.Add(Encoding.ASCII.GetString(Convert.FromBase64String(instructions.InsertSourceB64)));
            }
            if (File.Exists(instructions.OutputFileName))
            {
                File.Delete(instructions.OutputFileName);
            }
            Console.WriteLine("Compiling \"{0}\"...", instructions.MainClass);
            foreach (string item in instructions.Paths)
            {
                if (File.Exists(item))
                {
                    string code = File.ReadAllText(item);
                    sources.Add(code);
                }
            }

            List<MetadataReference> metadataReferences = new List<MetadataReference>();
            foreach (string assemblyLocation in instructions.AssemblyReferenceLocations.Distinct<string>())
            {
                try
                {
                    PortableExecutableReference metadata = MetadataReference.CreateFromFile(assemblyLocation, default(MetadataReferenceProperties), null);
                    metadataReferences.Add(metadata);
                }
                catch (Exception e)
                {
                    Reply(new CompilerReply
                    {
                        State = CompilationState.Error,
                        Message = "Assembly referencing error: " + e.Message,
                        Suspicious = true
                    });
                    return;
                }
            }

            List<SyntaxTree> trees = new List<SyntaxTree>();
            CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.CSharp7_3, DocumentationMode.Parse, SourceCodeKind.Regular, null);
            foreach (string item2 in sources.Distinct<string>())
            {
                try
                {
                    SyntaxTree tree = CSharpSyntaxTree.ParseText(item2, parseOptions, "", null, default(CancellationToken));
                    if (instructions.RejectShadyCode)
                    {
                        Console.WriteLine("Scanning tree...");
                        ScanTree(tree);
                    }
                    trees.Add(tree);
                }
                catch (ShadyException e2)
                {
                    Reply(new CompilerReply
                    {
                        State = CompilationState.Error,
                        Message = e2.Message,
                        Suspicious = true
                    });
                    return;
                }
                catch (Exception e3)
                {
                    Reply(new CompilerReply
                    {
                        State = CompilationState.Error,
                        Message = "Parsing error: " + e3.Message
                    });
                    return;
                }
            }

            if (trees.Count == 0)
            {
                Reply(new CompilerReply
                {
                    State = CompilationState.Error,
                    Message = "No source files could be parsed/found!"
                });
                return;
            }
            CSharpCompilation compilation = CSharpCompilation.Create(Guid.NewGuid().ToString().Normalize(), trees, metadataReferences, compilationOptions);
            ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics(default(CancellationToken));
            StringBuilder builder = new StringBuilder();

            foreach (Diagnostic item3 in diagnostics)
            {
                if (item3.Severity == DiagnosticSeverity.Error)
                {
                    AppendDiagnostic(builder, item3);
                }
            }
            if (builder.Length > 0)
            {
                Reply(new CompilerReply
                {
                    State = CompilationState.Error,
                    Message = builder.ToString()
                });
                return;
            }
            EmitResult emission = compilation.Emit(instructions.OutputFileName, null, null, null, null, default(CancellationToken));
            if (emission.Success)
            {
                Console.WriteLine("[{0}] - Success!", instructions.MainClass);
                Reply(new CompilerReply
                {
                    State = CompilationState.Success,
                    Message = null
                });
                return;
            }
            File.Delete(instructions.OutputFileName);
            builder.Clear();
            foreach (Diagnostic item4 in emission.Diagnostics)
            {
                if (item4.Severity == DiagnosticSeverity.Error)
                {
                    AppendDiagnostic(builder, item4);
                }
            }
        }

        private static void ScanTree(SyntaxTree tree)
        {
            IEnumerable<SyntaxNode> source = tree.GetRoot().DescendantNodes();
            if (source.OfType<ExternAliasDirectiveSyntax>().Any())
            {
                throw new Exception("External code is not allowed");
            }
            foreach (UsingDirectiveSyntax item in source.OfType<UsingDirectiveSyntax>())
            {
                string text = item.ToString();
                if (item.Alias != null)
                {
                    throw new Exception("Using directives with aliases are not allowed");
                }
                if (text.Contains("System.Security", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new ShadyException("Forbidden using directive: System.Security");
                }
                if (text.Contains("System.Web", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new ShadyException("Forbidden using directive: System.Web");
                }
                if (text.Contains("UnityEngine.Networking", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new ShadyException("Forbidden using directive: UnityEngine.Networking");
                }
                if (text.Contains("Steamworks", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new ShadyException("Forbidden using directive: Steamworks");
                }
            }
            foreach (IdentifierNameSyntax item2 in source.OfType<IdentifierNameSyntax>())
            {
                string text2 = item2.Identifier.Text;
                switch (text2)
                {
                    case "InteropServices":
                    case "Diagnostics":
                    case "Http":
                    case "CodeDom":
                    case "Application":
                    case "Quit":
                    case "UnityWebRequest":
                    case "TextReader":
                    case "TextWriter":
                    case "BinaryReader":
                    case "BinaryWriter":
                    case "StreamReader":
                    case "StreamWriter":
                    case "StringReader":
                    case "StringWriter":
                    case "FileStream":
                    case "IsolatedStorageFileStream":
                    case "NetworkStream":
                    case "PipeStream":
                    case "UserPreferenceManager":
                    case "WebRequest":
                    case "WebClient":
                    case "WebSocket":
                    case "Socket":
                    case "Steamworks":
                    case "Process":
                    case "DllImport":
                    case "LoadFile":
                    case "ReadFile":
                    case "WWW":
                    case "AppDomain":
                    case "AssemblyBuilder":
                    case "FromFile":
                    case "OpenURL":
                    case "LoadURL":
                    case "RejectShadyCode":
                    case "CreateType":
                    case "File":
                    case "FileInfo":
                    case "Directory":
                    case "DirectoryInfo":
                    case "Assembly":
                        throw new ShadyException(text2 + " is not allowed as an identifier");
                }
            }
        }

        private static void AppendDiagnostic(StringBuilder sb, Diagnostic diag)
        {
            LinePosition pos = diag.Location.GetMappedLineSpan().StartLinePosition;
            SyntaxTree sourceTree = diag.Location.SourceTree;
            string filename = ((sourceTree != null) ? sourceTree.FilePath : null) ?? "UNKNOWN FILE";
            if (!string.IsNullOrWhiteSpace(filename))
            {
                sb.Append("in");
                sb.Append(filename);
                sb.Append(", ");
            }
            sb.Append(diag.GetMessage(null));
            sb.Append(" at ");
            sb.Append(pos);
            sb.AppendLine();
        }

        [CompilerGenerated]
        internal static void MonitorLoop()
        {
            shouldStayAlive = false;
            server.Stop();
        }

        private const string quitMessage = "quit";

        private const string configPath = "config.json";

        private static bool shouldStayAlive = true;

        private static int gameProcessId = -1;

        private static WatsonTcpServer server;

        private static CSharpCompilationOptions compilationOptions;

        private static bool isBusy = false;

        private static string pathlol;

        private static Thread monitor;
    }
}
