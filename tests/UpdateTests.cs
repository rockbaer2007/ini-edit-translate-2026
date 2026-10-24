using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;
using Iet.Update;
using Ini_Edit___Translate;
using Newtonsoft.Json.Linq;

internal static class UpdateTests
{
    private static string root, runtime;
    private static int passed;
    private static void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
    private static void Test(string name, Action action) { action(); passed++; Console.WriteLine("PASS " + name); }
    private static void Fails(Action action) { bool failed = false; try { action(); } catch { failed = true; } Assert(failed); }
    private static string Folder() { string path = Path.Combine(root, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(path); return path; }
    private sealed class Handler : HttpMessageHandler
    {
        internal Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Reply;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel) { return Reply(request, cancel); }
    }
    private static string Metadata(string version = "2.1.38")
    {
        string prefix = "https://github.com/" + GithubUpdateClient.Repository + "/releases/download/v" + version + "/";
        return new JObject { ["tag_name"] = "v" + version, ["draft"] = false, ["prerelease"] = false, ["body"] = "Release notes",
            ["assets"] = new JArray(
                new JObject { ["name"] = "INI-Edit-Translate-2026-v" + version + "-windows.zip", ["browser_download_url"] = prefix + "INI-Edit-Translate-2026-v" + version + "-windows.zip" },
                new JObject { ["name"] = "SHA256SUMS.txt", ["browser_download_url"] = prefix + "SHA256SUMS.txt" }) }.ToString();
    }
    private static GithubUpdateClient Client(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        return new GithubUpdateClient(new Handler { Reply = (request, cancel) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) }) });
    }
    private static UpdateJob Package(string extra = null, bool omitMain = false)
    {
        string directory = Folder();
        string zip = Path.Combine(directory, "package.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        {
            if (!omitMain) archive.CreateEntryFromFile(Path.Combine(runtime, UpdateCore.MainExe), UpdateCore.MainExe);
            foreach (string name in new[] { UpdateCore.MainExe + ".config", "IET.Updater.exe", "Newtonsoft.Json.dll", "IniFile.dll",
                "help/Info_trans.exe", "help/FirebirdSql.Data.FirebirdClient.dll", "help/fbembed.dll", "help/IET2.FDB",
                "help/iconhelp.csv", "Info.rtf", "Info.en.rtf" })
            {
                using (var writer = new StreamWriter(archive.CreateEntry(name).Open())) writer.Write("NEW " + name);
            }
            if (extra != null)
                using (var writer = new StreamWriter(archive.CreateEntry(extra).Open())) writer.Write("EXTRA");
        }
        string target = Path.Combine(directory, "installation");
        Directory.CreateDirectory(Path.Combine(target, "help"));
        File.WriteAllText(Path.Combine(target, UpdateCore.MainExe), "OLD MAIN");
        File.WriteAllText(Path.Combine(target, UpdateCore.MainExe + ".config"), "OLD CONFIG");
        File.WriteAllText(Path.Combine(target, "help", "IET2.FDB"), "USER DATABASE");
        File.WriteAllText(Path.Combine(target, "help", "iconhelp.csv"), "USER WORDS");
        File.WriteAllText(Path.Combine(target, "Info.rtf"), "USER INFO");
        File.WriteAllText(Path.Combine(target, "my-file.ini"), "USER MODEL");
        return new UpdateJob { ZipPath = zip, Sha256 = UpdateCore.Hash(zip), InstallDirectory = target,
            BackupDirectory = Path.Combine(directory, "backup"), Version = FileVersionInfo.GetVersionInfo(Path.Combine(runtime, UpdateCore.MainExe)).FileVersion };
    }
    private static void Unchanged(UpdateJob job)
    {
        Assert(File.ReadAllText(Path.Combine(job.InstallDirectory, UpdateCore.MainExe)) == "OLD MAIN");
        Assert(File.ReadAllText(Path.Combine(job.InstallDirectory, "help", "IET2.FDB")) == "USER DATABASE");
    }
    private static void ProcessChecks()
    {
        Test("process enumeration failure never counts as confirmed exit", () =>
        {
            Fails(() => EditorProcess.WaitForAbsentProcess(123, 10, pid => { throw new System.ComponentModel.Win32Exception(5); }));
            Fails(() => EditorProcess.WaitForExit(new UpdateJob { ProcessId = 123 }, 10,
                (access, inherit, pid) => { throw new System.ComponentModel.Win32Exception(32); }));
        });
        foreach (string architecture in Environment.Is64BitOperatingSystem ? new[] { "x86", "x64" } : new[] { "x86" })
        {
            string directory = Folder();
            string executable = Path.Combine(directory, UpdateCore.MainExe);
            using (var compiler = new Microsoft.CSharp.CSharpCodeProvider())
            {
                var options = new System.CodeDom.Compiler.CompilerParameters
                { GenerateExecutable = true, OutputAssembly = executable, CompilerOptions = "/platform:" + architecture };
                var result = compiler.CompileAssemblyFromSource(options,
                    "class Fixture { static void Main(string[] args) { System.Threading.Thread.Sleep(int.Parse(args[0])); } }");
                Assert(!result.Errors.HasErrors);
            }
            Func<int, Process> start = milliseconds => Process.Start(new ProcessStartInfo(executable, milliseconds.ToString())
                { UseShellExecute = false, CreateNoWindow = true });
            Test("wait for real editor process across bitness " + architecture, () =>
            {
                using (var process = start(600))
                {
                    var job = new UpdateJob { InstallDirectory = directory, ProcessId = process.Id,
                        ProcessStartTicks = process.StartTime.ToUniversalTime().Ticks };
                    EditorProcess.WaitForExit(job, 10000);
                    Assert(process.WaitForExit(1000));
                    EditorProcess.WaitForExit(job, 1000); // Already exited, no module reads.
                }
            });
            Test("access denied cannot permit installation while editor exists " + architecture, () =>
            {
                using (var process = start(30000))
                {
                    try
                    {
                        var job = new UpdateJob { InstallDirectory = directory, ProcessId = process.Id,
                            ProcessStartTicks = process.StartTime.ToUniversalTime().Ticks };
                        Fails(() => EditorProcess.WaitForExit(job, 5, (access, inherit, pid) => { throw new System.ComponentModel.Win32Exception(5); }));
                        Assert(!process.HasExited);
                    }
                    finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(); } }
                }
            });
            Test("access denied waits for independently confirmed exit " + architecture, () =>
            {
                using (var process = start(600))
                {
                    var job = new UpdateJob { InstallDirectory = directory, ProcessId = process.Id,
                        ProcessStartTicks = process.StartTime.ToUniversalTime().Ticks };
                    EditorProcess.WaitForExit(job, 10000, (access, inherit, pid) => { throw new System.ComponentModel.Win32Exception(5); });
                    Assert(process.WaitForExit(1000));
                    Assert(!EditorProcess.ProcessExists(process.Id));
                }
            });
            Test("reject identity mismatch and timeout before installation " + architecture, () =>
            {
                using (var process = start(30000))
                {
                    try
                    {
                        var job = new UpdateJob { InstallDirectory = directory, ProcessId = process.Id,
                            ProcessStartTicks = process.StartTime.ToUniversalTime().Ticks + 1 };
                        Fails(() => EditorProcess.WaitForExit(job, 5));
                        job.ProcessStartTicks--;
                        job.InstallDirectory = Folder();
                        Fails(() => EditorProcess.WaitForExit(job, 5));
                        job.InstallDirectory = directory;
                        Fails(() => EditorProcess.WaitForExit(job, 5));
                        Assert(!process.HasExited);
                    }
                    finally { if (!process.HasExited) { process.Kill(); process.WaitForExit(); } }
                }
            });
        }
    }
    [STAThread]
    private static int Main(string[] args)
    {
        root = FolderRoot();
        runtime = Path.GetFullPath(args[0]);
        try
        {
            ProcessChecks();
            Test("public GitHub release and semantic version", () => { using (var client = Client(Metadata())) {
                var release = client.Latest(CancellationToken.None).GetAwaiter().GetResult();
                Assert(release.Version == new Version(2,1,38,0)); Assert(release.Notes == "Release notes"); } });
            Test("old version is not newer", () => Assert(new Version(UpdateCore.Normalize("2.1.9")) < new Version(UpdateCore.Normalize("2.1.38"))));
            Test("reject prerelease", () => { using(var client=Client(Metadata().Replace("\"prerelease\": false", "\"prerelease\": true"))) Fails(()=>client.Latest(CancellationToken.None).GetAwaiter().GetResult()); });
            Test("reject external asset URL", () => { using(var client=Client(Metadata().Replace("https://github.com/", "https://example.invalid/"))) Fails(()=>client.Latest(CancellationToken.None).GetAwaiter().GetResult()); });
            Test("reject HTTP and credentials in URLs", () => { Assert(!GithubUpdateClient.Trusted(new Uri("http://github.com/x"))); Assert(!GithubUpdateClient.Trusted(new Uri("https://user@github.com/x"))); });
            Test("HTTP error preserves application", () => { using(var client=Client("",HttpStatusCode.ServiceUnavailable)) Fails(()=>client.Latest(CancellationToken.None).GetAwaiter().GetResult()); });
            Test("cancel pending update request", () => {
                var cancellation=new CancellationTokenSource();
                using(var client=new GithubUpdateClient(new Handler{Reply=async(request,token)=> { await Task.Delay(10000,token); return new HttpResponseMessage(HttpStatusCode.OK); }})) {
                    var pending=client.Latest(cancellation.Token); cancellation.Cancel(); Fails(()=>pending.GetAwaiter().GetResult()); }
            });
            Test("download checksum mismatch is rejected", () => {
                var job=Package(); var bytes=File.ReadAllBytes(job.ZipPath);
                var values=new List<int>(); var phases=new List<UpdatePhase>();
                using(var client=new GithubUpdateClient(new Handler{Reply=(request,token)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){
                    Content=request.RequestUri.AbsolutePath.EndsWith(".txt") ? (HttpContent)new StringContent(new string('0',64)+"  test.zip") : new ByteArrayContent(bytes)})})) {
                    Fails(()=>client.Download(new GithubRelease{ZipName="test.zip",ZipUrl="https://github.com/test.zip",ChecksumsUrl="https://github.com/hash.txt",Version=new Version(job.Version)},Folder(),CancellationToken.None,new InlineProgress<int>(values.Add),new InlineProgress<UpdatePhase>(phases.Add)).GetAwaiter().GetResult());
                    Assert(!values.Contains(100) && phases.Contains(UpdatePhase.Validating) && !phases.Contains(UpdatePhase.Ready)); }
            });
            Test("verified download is staged before application shutdown",()=>{
                var job=Package(); var bytes=File.ReadAllBytes(job.ZipPath);
                var values=new List<int>(); var phases=new List<UpdatePhase>();
                using(var client=new GithubUpdateClient(new Handler{Reply=(request,token)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){
                    Content=request.RequestUri.AbsolutePath.EndsWith(".txt") ? (HttpContent)new StringContent(job.Sha256+"  test.zip") : new ByteArrayContent(bytes)})})) {
                    var prepared=client.Download(new GithubRelease{ZipName="test.zip",ZipUrl="https://github.com/test.zip",ChecksumsUrl="https://github.com/hash.txt",Version=new Version(job.Version)},Folder(),CancellationToken.None,new InlineProgress<int>(values.Add),new InlineProgress<UpdatePhase>(phases.Add)).GetAwaiter().GetResult();
                    Assert(prepared.Sha256==job.Sha256 && values.Last()==100 && values.Take(values.Count-1).All(value=>value<100));
                    Assert(phases.SequenceEqual(new[]{UpdatePhase.Downloading,UpdatePhase.Validating,UpdatePhase.Ready})); Unchanged(job); }
            });
            Test("redirect to another host is rejected",()=>{
                using(var client=new GithubUpdateClient(new Handler{Reply=(request,token)=>{
                    var response=new HttpResponseMessage(HttpStatusCode.Redirect);response.Headers.Location=new Uri("https://example.invalid/file"); return Task.FromResult(response);}}))
                    Fails(()=>client.Latest(CancellationToken.None).GetAwaiter().GetResult());
            });
            Test("corrupt archive rejected without application changes",()=>{
                var job=Package();File.WriteAllText(job.ZipPath,"not a ZIP");job.Sha256=UpdateCore.Hash(job.ZipPath);
                Fails(()=>UpdateCore.Install(job));Unchanged(job);
            });
            Test("archive symbolic link is rejected",()=>{
                var job=Package();
                using(var archive=ZipFile.Open(job.ZipPath,ZipArchiveMode.Update))
                    archive.GetEntry("IniFile.dll").ExternalAttributes=unchecked((int)0xa1ff0000);
                job.Sha256=UpdateCore.Hash(job.ZipPath);Fails(()=>UpdateCore.Install(job));Unchanged(job);
            });
            Test("new updater is staged from the verified package", () => {
                var job = Package(); string directory = Folder();
                string updater = UpdateCore.PrepareUpdater(job, directory);
                using (var zip = ZipFile.OpenRead(job.ZipPath))
                using (var input = zip.GetEntry("IET.Updater.exe").Open())
                using (var memory = new MemoryStream())
                {
                    input.CopyTo(memory);
                    Assert(File.ReadAllBytes(updater).SequenceEqual(memory.ToArray()));
                }
                Unchanged(job);
            });
            Test("tampered package cannot stage an updater", () => {
                var job = Package(); string directory = Folder();
                File.AppendAllText(job.ZipPath, "tampered");
                Fails(() => UpdateCore.PrepareUpdater(job, directory));
                Assert(!File.Exists(Path.Combine(directory, "IET.Updater.exe"))); Unchanged(job);
            });
            Test("changed archive rejected before writes", () => { var job=Package(); File.AppendAllText(job.ZipPath,"changed"); Fails(()=>UpdateCore.Install(job)); Unchanged(job); });
            foreach(string path in new[]{"../escape.exe","help/../../escape.exe","C:/escape.exe","/escape.exe","user.config","openai.json","help/../Info.rtf"})
                Test("reject package path "+path,()=>{ var job=Package(path); Fails(()=>UpdateCore.Install(job)); Unchanged(job); });
            Test("duplicate case-insensitive path rejected",()=>{ var job=Package("info.EN.rtf"); Fails(()=>UpdateCore.Install(job)); Unchanged(job); });
            Test("missing main executable rejected",()=>{ var job=Package(null,true); Fails(()=>UpdateCore.Install(job)); Unchanged(job); });
            Test("release and executable version must agree",()=>{ var job=Package(); job.Version="9.9.9.0"; Fails(()=>UpdateCore.Install(job)); Unchanged(job); });
            Test("cancel extraction before touching installation",()=>{ var job=Package(); var token=new CancellationToken(true); Fails(()=>UpdateCore.Extract(job,Folder(),token)); Unchanged(job); });
            Test("install preserves database, dictionaries, original help and user files",()=>{
                var job=Package(); UpdateCore.Install(job);
                Assert(UpdateCore.Hash(Path.Combine(job.InstallDirectory,UpdateCore.MainExe)) == UpdateCore.Hash(Path.Combine(runtime,UpdateCore.MainExe)));
                Assert(File.ReadAllText(Path.Combine(job.InstallDirectory,"help","IET2.FDB"))=="USER DATABASE");
                Assert(File.ReadAllText(Path.Combine(job.InstallDirectory,"help","iconhelp.csv"))=="USER WORDS");
                Assert(File.ReadAllText(Path.Combine(job.InstallDirectory,"Info.rtf"))=="USER INFO");
                Assert(File.ReadAllText(Path.Combine(job.InstallDirectory,"my-file.ini"))=="USER MODEL");
                Assert(File.ReadAllText(Path.Combine(job.BackupDirectory,"help","IET2.FDB"))=="USER DATABASE");
                Assert(File.ReadAllText(Path.Combine(job.BackupDirectory,UpdateCore.MainExe))=="OLD MAIN");
            });
            Test("partial write failure restores previous files",()=>{
                var job=Package(); Fails(()=>UpdateCore.Install(job,index=>{if(index==3)throw new IOException("simulated");}));
                Unchanged(job); Assert(File.ReadAllText(Path.Combine(job.InstallDirectory,UpdateCore.MainExe+".config"))=="OLD CONFIG");
                Assert(!File.Exists(Path.Combine(job.InstallDirectory,"IET.Updater.exe")));
                Assert(File.ReadAllText(Path.Combine(job.BackupDirectory,"result.txt")).Contains("restored"));
            });
            Test("locked executable prevents all writes",()=>{
                var job=Package();
                using(var locked=new FileStream(Path.Combine(job.InstallDirectory,UpdateCore.MainExe),FileMode.Open,FileAccess.Read,FileShare.Read))
                    Fails(()=>UpdateCore.Install(job));
                Unchanged(job);
            });
            Test("job round trip preserves process identity",()=>{ var job=Package(); job.ProcessId=123; job.ProcessStartTicks=456; var path=Path.Combine(Folder(),"job.json"); UpdateCore.WriteJob(path,job); var restored=UpdateCore.ReadJob(path); Assert(restored.ProcessId==123 && restored.ProcessStartTicks==456); });
            Test("active database prevents an inconsistent backup and installation",()=>{
                var job=Package();
                using(var locked=new FileStream(Path.Combine(job.InstallDirectory,"help","IET2.FDB"),FileMode.Open,FileAccess.Read,FileShare.Read))
                    Fails(()=>UpdateCore.Install(job));
                Unchanged(job);
            });
            foreach(string locale in new[]{"de","en","fr"})
                Test("update dialog renders without real network "+locale,()=>{
                    Thread.CurrentThread.CurrentUICulture=CultureInfo.GetCultureInfo(locale);
                    Application.EnableVisualStyles();
                    using(var client=Client(Metadata("2.1.38")))
                    using(var form=new GithubUpdateForm(client)){
                        form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.ShowInTaskbar=false;
                        form.Show();Application.DoEvents();form.Refresh();
                        Assert(!string.IsNullOrWhiteSpace(form.Text));
                        using(var bitmap=new Bitmap(form.Width,form.Height)){
                            form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height));
                            bitmap.Save(Path.Combine(root,"update-"+locale+".png"));
                        }
                        form.Close();
                    }
                });
            if(args.Length>1 && args[1]=="--live-check")
                using(var client=new GithubUpdateClient()){
                    var latest=client.Latest(CancellationToken.None).GetAwaiter().GetResult();
                    Console.WriteLine("READ ONLY live GitHub release: "+latest.Version);
                    using(var form=new GithubUpdateForm()){
                        form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.ShowInTaskbar=false;
                        form.Show();
                        var label=form.Controls[0].Controls.OfType<Label>().Single();
                        var timer=Stopwatch.StartNew();
                        while(!label.Text.Contains(latest.Version.ToString()) && timer.ElapsedMilliseconds<20000){
                            Application.DoEvents();Thread.Sleep(20);
                        }
                        Assert(label.Text.Contains(latest.Version.ToString()));
                        using(var bitmap=new Bitmap(form.Width,form.Height)){
                            form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height));
                            bitmap.Save(Path.Combine(root,"live-update-dialog.png"));
                        }
                        form.Close();
                    }
                    Console.WriteLine("PASS default update dialog reads GitHub without installation");
                }
            Console.WriteLine(passed+" update checks passed; no real installation modified.");
            if(args.Length>2 && args[1]=="--package"){
                var package=new UpdateJob{ZipPath=Path.GetFullPath(args[2]),Sha256=UpdateCore.Hash(args[2]),
                    Version=FileVersionInfo.GetVersionInfo(Path.Combine(runtime,UpdateCore.MainExe)).FileVersion};
                UpdateCore.Extract(package,Folder());
                Console.WriteLine("PASS actual portable release is accepted by updater");
            }
            Console.WriteLine("Artifacts: "+root);
            return 0;
        }
        catch(Exception error){Console.Error.WriteLine(error);return 1;}
    }
    private static string FolderRoot() { var folder=Path.Combine(Path.GetTempPath(),"iet-update-tests-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder); return folder; }
}
