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
using Newtonsoft.Json;
using WatsonTcp;

namespace PPGModCompiler
{
    public class CompilerConfig
    {
        public string Hostname;
        public string SteamPath;

        public int Port;

        public CompilerConfig()
        {
            Hostname = "127.0.0.1";
            SteamPath = null;
            Port = 32513;
        }
    }

    public enum CompilationState
    {
        Unknown,
        Success,
        Error
    }

    public struct CompilerReply
    {
        public int ID;

        public CompilationState State;

        public string Message;

        public bool Suspicious;
    }

    public class Compiler
    {
        [Obsolete] // I'm assuming this was added by devs, but like... Why use a seperate mod compile server?
                   // Why not just build within the game itself? Why outsource the build code to a local server?
                   // Wtf devs?!
                   // (Please keep using an external server though - it helps the people who are on Mac or Linux devices compile mods natively from in-game)
        public static int Main(string[] args)
        {
            CompilerConfig config = ReadConfig();
            SteamPath = Path.GetFullPath(config.SteamPath);

            // Check if we gud
            if (File.Exists(SteamPath)) throw new ArgumentException("SteamPath was a file. Please set SteamPath to a directory with the workshop and common folders in it.");
            if (!(Directory.Exists(SteamPath) && Directory.Exists(Path.Combine(SteamPath, "workshop")) && Directory.Exists(Path.Combine(SteamPath, "common")))) throw new FileNotFoundException("Couldn't find workshop or common folders in SteamPath directory. Please update SteamPath to be the root Steam directory!");

            // Hope we are gud after this. I don't give a f**k anyway as CompileMod will just fail anyway if we don't have the folders we are looking for.

            compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, reportSuppressedDiagnostics: false, null, null, null, null, OptimizationLevel.Release);

            server = new WatsonTcpServer(config.Hostname, config.Port);

            WatsonTcpServerSettings settings = server.Settings;
            settings.Logger = (Action<Severity, string>)Delegate.Combine(settings.Logger, new Action<Severity, string>(delegate (Severity s, string m)
            {
                Console.WriteLine("[{0}] {1}", s, m);
            }));
            server.Events.ClientConnected += delegate (object o, ConnectionEventArgs e)
            {
                Console.WriteLine("Client {0} connected", e.Client.Guid);

                server.SendAndWaitAsync(3000, e.Client.Guid, "alles goed ouwe", null);
            };
            server.Events.ClientDisconnected += delegate (object o, DisconnectionEventArgs e)
            {
                Console.WriteLine("Client {0} disconnected", e.Client.Guid);
            };
            server.Events.MessageReceived += delegate (object o, MessageReceivedEventArgs e)
            {
                Console.WriteLine("Received message from {0}", e.Client.Guid);
                ProcessMessage(e.Data, e.Client.Guid);
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
            catch (System.Net.Sockets.SocketException err)
            {
                Console.WriteLine("An error has occurred - A server may be already running. If so, please quit it and try again. If PPG is open, please close it.");
                Console.WriteLine(err.Message);
                Console.Beep();
                return 69; // I'm sure this'll help that one person creating a wrapper script lmfao
            } catch (Exception err)
            {
                Console.WriteLine("An unknown error has occurred. Please report to StupidRepo on GitHub!");
                Console.WriteLine(err.Message);
                Console.Beep();
                return 420; // This too
            }

            Console.WriteLine($"Started listening on {config.Hostname}:{config.Port}");
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
            throw new Exception("Can't find config.json - this is REQUIRED as it holds the location of your Steam directory!");
            return new CompilerConfig();
        }

        private static void ProcessMessage(byte[] data, Guid remote)
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
                    text = text.Replace("C:\\\\Program Files (x86)\\\\Steam\\\\steamapps\\\\workshop", Path.Combine(SteamPath, "workshop"))
                        .Replace("C:\\\\Program Files (x86)\\\\Steam\\\\steamapps\\\\common\\\\People Playground\\\\", Path.Combine(SteamPath, "common/People Playground"))
                        .Replace("\\\\", "/");
                    Console.WriteLine("Received mod compile instructions!");
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

        private static void CompileMod(ModCompileInstructions instructions, Guid remote)
        {
            void Reply(CompilerReply reply) // ADHD is kicking in and is making me forget what this does every time I look at it so I'm commenting it
            {
                reply.ID = instructions.ID; // Gets the mod ID from instructions (ModCompileInstructions) variable
                Task.Run(async delegate {
                    string data = JsonConvert.SerializeObject(reply); // Converts reply into JSON
                    try
                    {
                        await server.SendAsync(remote, data); // Sends reply
                        Console.WriteLine("Sent {0}: {1} to {2}", reply.State, reply.Message, remote); // Sends to Console that we sent a reply
                    }
                    catch (Exception arg)
                    {
                        Console.Error.WriteLine("Failed to send reply to {1}: {0}", arg, remote); // ...
                    }
                });
            }
            List<string> sources = new List<string>();
            //File.WriteAllText("last_instructions", JsonConvert.SerializeObject(instructions, Formatting.Indented));
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
            // So we return here, and error to the client if there was an error found
            if (builder.Length > 0)
            {
                Reply(new CompilerReply
                {
                    State = CompilationState.Error,
                    Message = builder.ToString()
                });
                return;
            }
            // If no error, we compile. If succeeded, we return and success to the client
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
            // If not succeeded, we find the error(s) in the emission
            File.Delete(instructions.OutputFileName);
            builder.Clear();
            foreach (Diagnostic item4 in emission.Diagnostics)
            {
                if (item4.Severity == DiagnosticSeverity.Error)
                {
                    AppendDiagnostic(builder, item4);
                }
            }
            // Cool so we cleared the builder and appended but now what? We just leave the client hanging?
            // Wtf devs?
            // I'm just gonna add my own code in for this rare case then.
            Reply(new CompilerReply
            {
                State = CompilationState.Error,
                Message = builder.ToString()
            });
            return;
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
                if (text.Contains("System.Reflection", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new ShadyException("Forbidden using directive: System.Reflection");
                }
            }
            foreach (IdentifierNameSyntax IDNSyntax in source.OfType<IdentifierNameSyntax>())
            {
                string id = IDNSyntax.Identifier.Text;
                switch (id)
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
                    case "MethodInfo":
                    case "GetMethod":
                    case "GetMethods":
                    case "Assembly":
                        throw new ShadyException(id + " is not allowed as an identifier");
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

        private const string quitMessage = "quit";

        private const string configPath = "config.json";

        private static bool shouldStayAlive = true;

        private static WatsonTcpServer server;

        private static CSharpCompilationOptions compilationOptions;

        private static bool isBusy = false;

        private static string SteamPath;
    }
}
