using GitSynchronizationApi.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitSynchronizationApi.Services
{
    public class SynchronizeModel {
        [Required]
        public string SourceUrl { get; set; }
        [Required]
        public string DestinationUrl { get; set; }
    }
    public interface IGitSynchronizeService {
        public static bool IsSynchrozing { get; set; }
        Task SyncRepository(string sourceUrl, string destinationUrl, string repoContainerPath);
    }
    public class GitSynchronizeService: IGitSynchronizeService
    {
        public static bool IsSynchrozing { get; set; }
        public async Task SyncRepository(string sourceUrl, string destinationUrl, string repoContainerPath)
        {
            ValidateSyncProcess();

            IsSynchrozing = true;
            try
            {
                var sourcePath = await PullCode(sourceUrl, repoContainerPath);
                await PushCodeToRemote(sourcePath, destinationUrl, repoContainerPath);
            }
            catch (Exception e)
            {
                IsSynchrozing = false;
                throw;
            }
            IsSynchrozing = false;
        }
        private async Task<string> PullCode(string sourceUrl, string repoContainerPath)
        {
            try
            {
                WriteDebugDashedLine();
                WriteDebugOutPut("Start pull code");
                var repoSourceName = sourceUrl.Split("/").LastOrDefault()?.Split(".").FirstOrDefault();

                var sourcePath = Path.Combine(repoContainerPath, repoSourceName);

                var scriptContents = new StringBuilder();
                scriptContents.AppendLine("Param($RepoContainerPath, $RepoSource)");
                // 1. cd repo container 
                scriptContents.AppendLine($"cd {repoContainerPath}");

                // 2. kiểm tra đã có repoSource
                EnsureHasGitRepo(sourceUrl, sourcePath, scriptContents);

                var scriptParameters = new Dictionary<string, object>() {
                    { "RepoContainerPath", repoContainerPath },
                    { "RepoSource", repoSourceName }
            };

                var hosted = new HostedRunspace();
                await hosted.RunScript(scriptContents.ToString(), scriptParameters);
                WriteDebugDashedLine();
                return sourcePath;
            }
            catch (Exception e)
            {
                IsSynchrozing = false;
                throw;
            }
        }

        private async Task PushCodeToRemote(string fromLocalRepo, string destinationUrl, string repoContainerPath)
        {
            try
            {
                WriteDebugDashedLine();
                WriteDebugOutPut("Start push code");
                var repoDestinationName = destinationUrl.Split("/").LastOrDefault()?.Split(".").FirstOrDefault();

                var sourcePath = Path.Combine(repoContainerPath, fromLocalRepo);
                var destinationPath = Path.Combine(repoContainerPath, repoDestinationName);

                var scriptContents = new StringBuilder();
                scriptContents.AppendLine("Param($RepoContainerPath, $RepoDestination)");
                //1. cd repo container 
                scriptContents.AppendLine($"cd {repoContainerPath}");

                //2. kiểm tra đã có repoDestination
                EnsureHasGitRepo(destinationUrl, destinationPath, scriptContents);

                // 3. copy code từ repoSource -> repoDestination
                CopyResource(from: sourcePath, to: destinationPath, scriptContents);

                // 4. push code trong repoDestination lên remote
                PushCodeToRemote(destinationPath, scriptContents);

                // 5. Get git log
                GetGitLog(scriptContents);
                WriteDashedLine(scriptContents);

                var scriptParameters = new Dictionary<string, object>() {
                    { "RepoContainerPath", repoContainerPath },
                    { "RepoDestination", repoDestinationName }
            };

                var hosted = new HostedRunspace();
                await hosted.RunScript(scriptContents.ToString(), scriptParameters);
                WriteDebugDashedLine();
            }
            catch (Exception e)
            {
                IsSynchrozing = false;
                throw;
            }
            
        }
        
        //public async Task SyncRepository(string sourceUrl, string destinationUrl, string repoContainerPath)
        //{
        //    var repoSourceName = sourceUrl.Split("/").LastOrDefault()?.Split(".").FirstOrDefault();
        //    var repoDestinationName = destinationUrl.Split("/").LastOrDefault()?.Split(".").FirstOrDefault();

        //    var sourcePath = Path.Combine(repoContainerPath, repoSourceName);
        //    var destinationPath = Path.Combine(repoContainerPath, repoDestinationName);

        //    var scriptContents = new StringBuilder();
        //    scriptContents.AppendLine("Param($RepoContainerPath, $RepoSource, $RepoDestination)");
        //    //scriptContents.AppendLine($"if (Test-Path -Path {sourcePath}) {{ \"sourcePath exists!\" }} else {{ \"sourcePath doesn't exist.\" }}");
        //    //1. cd repo container 
        //    scriptContents.AppendLine($"cd {repoContainerPath}");

        //    //2.kiem tra đã có repoSource, repoDestination
        //    EnsureHasGitRepo(sourceUrl, sourcePath, scriptContents);
        //    EnsureHasGitRepo(destinationUrl, destinationPath, scriptContents);

        //    // 3. copy code từ repoSource -> repoDestination
        //    CopyResource(from: sourcePath, to: destinationPath, scriptContents);

        //    // 4. push code trong repoDestination lên remote
        //    PushCodeToRemote(destinationPath, scriptContents);

        //    // 5. Get git log
        //    GetGitLog(scriptContents);
        //    WriteDashedLine(scriptContents);
        //    WriteOutPut("RepoContainerPath: $RepoContainerPath", scriptContents);
        //    WriteOutPut("RepoSource: $RepoSource", scriptContents);
        //    WriteOutPut("RepoDestination: $RepoDestination", scriptContents);

        //    var scriptParameters = new Dictionary<string, object>() {
        //            { "RepoContainerPath", repoContainerPath },
        //            { "RepoSource", repoSourceName },
        //            { "RepoDestination", repoDestinationName }
        //    };

        //    var hosted = new HostedRunspace();
        //    await hosted.RunScript(scriptContents.ToString(), scriptParameters);

        //    Console.WriteLine("Script execution completed. Press enter key to exit:");
        //}

        private void EnsureHasGitRepo(string sourceUrl, string sourcePath, StringBuilder scriptContents)
        {
            WriteDashedLine(scriptContents);
            WriteOutPut($"Check repository {sourcePath}", scriptContents);
            scriptContents.AppendLine($"if (Test-Path -Path {sourcePath}) {{");

            scriptContents.AppendLine($"Pull code from remote: {sourceUrl}");
            scriptContents.AppendLine($"cd {sourcePath}");
            scriptContents.AppendLine($"Get-Location");
            scriptContents.AppendLine($"git pull");

            scriptContents.AppendLine($"}} else {{");

            scriptContents.AppendLine($"Clone repository: {sourceUrl}");
            scriptContents.AppendLine($"Get-Location");
            scriptContents.AppendLine($"git clone {sourceUrl}");

            scriptContents.AppendLine($"}}");
            WriteDashedLine(scriptContents);
        }

        private void CopyResource(string from, string to, StringBuilder scriptContents)
        {
            WriteDashedLine(scriptContents);
            WriteOutPut($"Coping...", scriptContents);
            scriptContents.AppendLine($"Copy-Item -Path \"{from }/*\" -Destination \"{to}\" -Recurse -Exclude \"*.git\"");
            WriteDashedLine(scriptContents);
        }

        private void PushCodeToRemote(string repoPath, StringBuilder scriptContents)
        {
            WriteDashedLine(scriptContents);
            WriteOutPut($"Push code to remote at: [{repoPath}]", scriptContents);
            var guid = Guid.NewGuid().ToString();
            scriptContents.AppendLine($"cd {repoPath}");
            scriptContents.AppendLine($"git add .");
            scriptContents.AppendLine($"git commit -m \"{guid}-sync {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}\"");
            scriptContents.AppendLine($"git push");

            scriptContents.AppendLine($"$GitLog = git log");
            scriptContents.AppendLine($"if ([bool]($GitLog  -like \"*{guid}*\")) {{");

            scriptContents.AppendLine($"Write-Output \"Sync success!\"");

            scriptContents.AppendLine($"}} else {{");

            scriptContents.AppendLine($"Write-Output \"Sync failed!\"");

            scriptContents.AppendLine($"}}");
            WriteDashedLine(scriptContents);
        }

        private void GetGitLog(StringBuilder scriptContents)
        {
            WriteDashedLine(scriptContents);
            WriteOutPut($"Commit log:", scriptContents);
            scriptContents.AppendLine($"git log");
            WriteDashedLine(scriptContents);
        }

        private void WriteOutPut(string content, StringBuilder scriptContents)
        {
            scriptContents.AppendLine($"Write-Output \"\t- {content}\"");
        }
        private void WriteDashedLine(StringBuilder scriptContents)
        {
            scriptContents.AppendLine($"Write-Output \"=========================================\"");
        }

        private void WriteDebugDashedLine()
        {
            Debug.WriteLine($"\"=========================================\"");
        }
        private void WriteDebugOutPut(string content)
        {
            Debug.WriteLine($"\t- {content}");
        }
        private void ValidateSyncProcess()
        {
            if (IsSynchrozing) throw new Exception("Git synchronize processing, try later.");
        }
    }
}
