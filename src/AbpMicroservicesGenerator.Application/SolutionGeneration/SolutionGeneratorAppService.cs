using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace AbpMicroservicesGenerator.SolutionGeneration
{
    public class SolutionGeneratorAppService : AbpMicroservicesGeneratorAppService, ISolutionGeneratorAppService
    {
        private static readonly Dictionary<Guid, SolutionGenerationResponse> _generationCache = new();
        private readonly IConfiguration _configuration;
        private readonly string _outputPath;

        public SolutionGeneratorAppService(IConfiguration configuration)
        {
            _configuration = configuration;

            // إنشاء مجلد للحلول المولدة
            // يمكن تخصيص المسار من appsettings.json
            var customPath = _configuration["SolutionGenerator:OutputPath"];
            _outputPath = !string.IsNullOrEmpty(customPath)
                ? customPath
                : Path.Combine(Directory.GetCurrentDirectory(), "GeneratedSolutions");

            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
            }
        }
        /// <summary>
        /// 1
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<SolutionGenerationResponse> GenerateSolutionAsync(SolutionGenerationRequest request)
        {
            try
            {
                Logger.LogInformation("Starting solution generation for: {SolutionName}", request.SolutionName);

                var response = new SolutionGenerationResponse
                {
                    Id = Guid.NewGuid(),
                    SolutionName = request.SolutionName,
                    Status = GenerationStatus.InProgress,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Starting solution generation...",
                    CurrentStep = "Initializing...",
                    Progress = 0
                };

                _generationCache[response.Id] = response;
                Logger.LogInformation("Added generation {Id} to cache. Cache now contains {Count} items.", response.Id, _generationCache.Count);

                _ = Task.Run(async () => await GenerateSolutionInternalAsync(request, response));

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error starting solution generation");
                return new SolutionGenerationResponse
                {
                    Id = Guid.NewGuid(),
                    SolutionName = request.SolutionName,
                    Status = GenerationStatus.Failed,
                    Message = $"Failed to start generation: {ex.Message}",
                    CreatedAt = DateTime.UtcNow,
                    Errors = new List<string> { ex.Message }
                };
            }
        }
        /// <summary>
        /// 2
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        private async Task GenerateSolutionInternalAsync(SolutionGenerationRequest request, SolutionGenerationResponse response)
        {
            var solutionPath = Path.Combine(_outputPath, request.SolutionName);

            try
            {
                Logger.LogInformation("Starting internal generation for {SolutionName} at {SolutionPath}", request.SolutionName, solutionPath);

                response.CurrentStep = "Creating solution directory...";
                response.Progress = 5;
                Directory.CreateDirectory(solutionPath);
                Logger.LogInformation("Created solution directory: {SolutionPath}", solutionPath);
                await Task.Delay(500);

                var mainSolutionName = $"{request.SolutionName}";
               // var mainSolutionName = $"{request.SolutionName}";
                var mainSolutionPath = Path.Combine(solutionPath, $"{mainSolutionName}.sln");
                await CreateMainSolutionAsync(mainSolutionPath);

                response.CurrentStep = "Creating root projects...";
                response.Progress = 10;
                await GenerateRootFolderProjectsAsync(solutionPath, request);


                response.CurrentStep = "Creating project structure...";
                response.Progress = 15;
                await CreateProjectStructureAsync(solutionPath, request);

            
                //response.CurrentStep = "Adding projects to main solution...";
                //response.Progress = 90;
                //await AddProjectToSolutionAsync(mainSolutionPath, solutionPath, mainSolutionName);





                response.Status = GenerationStatus.Completed;
                response.Progress = 100;
                response.CurrentStep = "Completed successfully!";
                response.CompletedAt = DateTime.UtcNow;
                response.Message = "Solution generated successfully!";
                response.GeneratedFiles = GetGeneratedFilesList(solutionPath);
                response.DownloadUrl = $"/api/solution-generator/download/{response.Id}";

                Logger.LogInformation("Solution generated at: {SolutionPath}", solutionPath);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during solution generation");
                response.Status = GenerationStatus.Failed;
                response.Message = $"Generation failed: {ex.Message}";
                response.Errors.Add(ex.Message);
                response.CompletedAt = DateTime.UtcNow;

                if (Directory.Exists(solutionPath))
                {
                    try { Directory.Delete(solutionPath, true); } catch { }
                }
            }
        }

        private async Task CreateProjectStructureAsync(string solutionPath, SolutionGenerationRequest request)
        {
            Logger.LogInformation($"Creating project structure at: {solutionPath}");

            // Create top-level folders
            //var topLevelFolders = new[] { "shared", "gateways", "apps", "services" };
            //foreach (var folder in topLevelFolders)
            //{
            //    var fullPath = Path.Combine(solutionPath, folder);
            //    Directory.CreateDirectory(fullPath);
            //    Logger.LogInformation($"Created top-level folder: {fullPath}");
            //}


            if (request.Microservices == null || !request.Microservices.Any())
            {
                Logger.LogWarning("No microservices provided in the request. Skipping project structure creation.");
                return;
            }

            var servicesPath = Path.Combine(solutionPath, "services");
            Directory.CreateDirectory(servicesPath);
            Logger.LogInformation($"Created services folder: {servicesPath}");

            foreach (var service in request.Microservices)
            {
                if (string.IsNullOrEmpty(service.Name))
                {
                    Logger.LogWarning("Microservice name is empty. Skipping.");
                    continue;
                }

                var servicePath = Path.Combine(servicesPath, service.Name);
                Directory.CreateDirectory(servicePath);
                Logger.LogInformation($"Created service folder: {servicePath}");

                var serviceSubFolders = new[] { "src", "host", "test" };
                foreach (var subFolder in serviceSubFolders)
                {
                    var subFolderPath = Path.Combine(servicePath, subFolder);
                    Directory.CreateDirectory(subFolderPath);
                    Logger.LogInformation($"Created service subfolder: {subFolderPath}");

                    await GenerateMicroserviceProjectsAsync(subFolderPath, request, service, subFolder, solutionPath);
                }

                await GenerateSolutionFileAsync(servicePath, request, service);
            }

            await Task.CompletedTask;
        }

        private async Task GenerateMicroserviceProjectsAsync(string subFolderPath, SolutionGenerationRequest request, MicroserviceRequest microservice, string subFolderType, string mainSolutionDirectory)
        {
            Logger.LogInformation($"Generating projects for microservice: {microservice.Name} in {subFolderType}");

            var projects = subFolderType switch
            {
                "src" => new[]
                {
                $"{request.CompanyName}.{microservice.Name}.Domain.Shared",
                $"{request.CompanyName}.{microservice.Name}.Domain",
                $"{request.CompanyName}.{microservice.Name}.Application.Contracts",
                $"{request.CompanyName}.{microservice.Name}.Application",
                $"{request.CompanyName}.{microservice.Name}.EntityFrameworkCore",
                $"{request.CompanyName}.{microservice.Name}.HttpApi",
                $"{request.CompanyName}.{microservice.Name}.HttpApi.Client"
            },
                "host" => new[]
                {
                $"{request.CompanyName}.{microservice.Name}.HttpApi.Host"
            },
                "test" => new[]
                {
                $"{request.CompanyName}.{microservice.Name}.Tests"
            },
                _ => Array.Empty<string>()
            };

            var serviceFolderPath = Path.Combine(mainSolutionDirectory, "services", microservice.Name);
            var microserviceSolutionPath = Path.Combine(serviceFolderPath, $"{request.CompanyName}.{microservice.Name}.sln");
            var mainSolutionPath = Path.Combine(mainSolutionDirectory, $"{request.SolutionName}.sln");

            foreach (var project in projects)
            {
                var projectType = GetProjectTypeFromName(project);
                var projectContent = subFolderType == "host"
                    ? GenerateHostProjectContent(project, microservice, request)
                    : GenerateProjectFileContent(project, microservice, projectType);

                var projectFilePath = Path.Combine(subFolderPath, $"{project}.csproj");
                await File.WriteAllTextAsync(projectFilePath, projectContent);
                Logger.LogInformation($"Created project file: {projectFilePath}");

                await GenerateBasicProjectFiles(subFolderPath, project, request, microservice);

                var solutionFolder = $"services\\{microservice.Name}\\{subFolderType}";
                //  var relativeProjectPath = Path.GetRelativePath(mainSolutionDirectory, projectFilePath);
                await AddProjectToSolutionAsync(mainSolutionPath, solutionFolder, projectFilePath);
                await AddProjectToSolutionAsync(microserviceSolutionPath, subFolderType, projectFilePath);
            }

            if (subFolderType == "host")
            {
                await GenerateHostFiles(subFolderPath, $"{request.CompanyName}.{microservice.Name}.HttpApi.Host", request, microservice);
            }
        }
        private async Task GenerateSolutionFileAsync(string servicePath, SolutionGenerationRequest request, MicroserviceRequest microservice)
        {
            Logger.LogInformation($"Generating solution file for microservice: {microservice.Name}");

            var solutionName = $"{request.CompanyName}.{microservice.Name}";
            var solutionFilePath = Path.Combine(servicePath, $"{solutionName}.sln");
            var solutionContent = new StringBuilder();

            solutionContent.AppendLine();
            solutionContent.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            solutionContent.AppendLine("# Visual Studio Version 17");
            solutionContent.AppendLine("VisualStudioVersion = 17.0.31903.59");
            solutionContent.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");

            var subFolders = new[] { "src", "host", "test" };
            var projectGuids = new Dictionary<string, Guid>();
            var solutionFoldersGuids = new Dictionary<string, Guid>();
            foreach (var subFolder in subFolders)
            {
                var subFolderPath = Path.Combine(servicePath, subFolder);
                if (!Directory.Exists(subFolderPath))
                {
                    Logger.LogWarning($"Subfolder {subFolderPath} does not exist. Skipping.");
                    continue;
                }

                var solutionFolderGuid = Guid.NewGuid();
                solutionFoldersGuids[subFolder] = solutionFolderGuid;

                solutionContent.AppendLine($"Project(\"{{2150E333-8FDC-42A3-9474-1A3956D46DE8}}\") = \"{subFolder}\", \"{subFolder}\", \"{{{solutionFolderGuid}}}\"");
                solutionContent.AppendLine("EndProject");

                var csprojFiles = Directory.GetFiles(subFolderPath, "*.csproj");
                foreach (var csprojFile in csprojFiles)
                {
                    var projectName = Path.GetFileNameWithoutExtension(csprojFile);
                    var projectGuid = Guid.NewGuid();
                    projectGuids[projectName] = projectGuid;

                    var relativeProjectPath = Path.Combine(subFolder, Path.GetFileName(csprojFile)).Replace('/', '\\');
                    solutionContent.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{projectName}\", \"{relativeProjectPath}\", \"{{{projectGuid}}}\"");
                    solutionContent.AppendLine("EndProject");
                }
            }

            solutionContent.AppendLine("Global");
            solutionContent.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
            solutionContent.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
            solutionContent.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
            solutionContent.AppendLine("\tEndGlobalSection");
            solutionContent.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

            foreach (var project in projectGuids)
            {
                solutionContent.AppendLine($"\t\t{{{project.Value}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
                solutionContent.AppendLine($"\t\t{{{project.Value}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
                solutionContent.AppendLine($"\t\t{{{project.Value}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");
                solutionContent.AppendLine($"\t\t{{{project.Value}}}.Release|Any CPU.Build.0 = Release|Any CPU");
            }

            solutionContent.AppendLine("\tEndGlobalSection");
            solutionContent.AppendLine("\tGlobalSection(SolutionProperties) = preSolution");
            solutionContent.AppendLine("\t\tHideSolutionNode = FALSE");
            solutionContent.AppendLine("\tEndGlobalSection");
            solutionContent.AppendLine("\tGlobalSection(NestedProjects) = preSolution");

            foreach (var subFolder in subFolders)
            {
                var subFolderPath = Path.Combine(servicePath, subFolder);
                if (!Directory.Exists(subFolderPath)) continue;

                var csprojFiles = Directory.GetFiles(subFolderPath, "*.csproj");
                foreach (var csprojFile in csprojFiles)
                {
                    var projectName = Path.GetFileNameWithoutExtension(csprojFile);
                    if (projectGuids.ContainsKey(projectName) && solutionFoldersGuids.ContainsKey(subFolder))
                    {
                        var projectGuid = projectGuids[projectName];
                        var folderGuid = solutionFoldersGuids[subFolder];

                        solutionContent.AppendLine($"\t\t{{{projectGuid}}} = {{{folderGuid}}}");
                    }
                }
            }
            solutionContent.AppendLine("\tEndGlobalSection");
            solutionContent.AppendLine("EndGlobal");

            await File.WriteAllTextAsync(solutionFilePath, solutionContent.ToString());
            Logger.LogInformation($"Created solution file: {solutionFilePath}");
        }
        private async Task AddProjectToSolutionAsync(string solutionPath, string solutionFolder, string projectPath)
        {
            if (!File.Exists(solutionPath))
            {
                Logger.LogWarning($"Solution file not found: {solutionPath}");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"sln \"{solutionPath}\" add --solution-folder \"{solutionFolder}\" \"{projectPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                Logger.LogError($"❌ Failed to add project to solution: {error}");
            }
            else
            {
                Logger.LogInformation($"✅ Project added to solution under '{solutionFolder}': {projectPath}");
                Logger.LogInformation($"dotnet output: {output}");
            }
        }





        private async Task CreateMainSolutionAsync(string solutionPath)
        {
            if (File.Exists(solutionPath))
            {
                Logger.LogInformation($"Main solution already exists: {solutionPath}");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"new sln -n \"{Path.GetFileNameWithoutExtension(solutionPath)}\"",
                WorkingDirectory = Path.GetDirectoryName(solutionPath),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                Logger.LogError($"Failed to create main solution: {error}");
            }
            else
            {
                Logger.LogInformation($"Main solution created: {solutionPath}");
            }
        }



   


        private string GetProjectTypeFromName(string projectName)
        {
            if (projectName.EndsWith(".Domain.Shared")) return "DomainShared";
            if (projectName.EndsWith(".Domain")) return "Domain";
            if (projectName.EndsWith(".Application.Contracts")) return "ApplicationContracts";
            if (projectName.EndsWith(".Application")) return "Application";
            if (projectName.EndsWith(".EntityFrameworkCore")) return "EntityFrameworkCore";
            if (projectName.EndsWith(".HttpApi")) return "HttpApi";
            if (projectName.EndsWith(".HttpApi.Client")) return "HttpApiClient";
            if (projectName.EndsWith(".HttpApi.Host")) return "HttpApiHost";
            if (projectName.EndsWith(".Tests")) return "Tests";
            return "Unknown";
        }

        private string GenerateProjectFileContent(string projectName, MicroserviceRequest microservice, string projectType)
        {
            var content = new StringBuilder();
            content.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
            content.AppendLine("  <PropertyGroup>");
            content.AppendLine($"    <TargetFramework>net8.0</TargetFramework>");
            content.AppendLine($"    <RootNamespace>{projectName}</RootNamespace>");
            content.AppendLine("  </PropertyGroup>");

            switch (projectType)
            {
                case "DomainShared":
                    content.AppendLine("  <ItemGroup>");
                    content.AppendLine("    <PackageReference Include=\"Volo.Abp.Core\" Version=\"8.0.0\" />");
                    content.AppendLine("  </ItemGroup>");
                    break;
                case "Domain":
                    content.AppendLine("  <ItemGroup>");
                    content.AppendLine($"    <ProjectReference Include=\"..\\{projectName.Replace(".Domain", ".Domain.Shared")}.csproj\" />");
                    content.AppendLine("    <PackageReference Include=\"Volo.Abp.Ddd\" Version=\"8.0.0\" />");
                    content.AppendLine("  </ItemGroup>");
                    break;
                case "ApplicationContracts":
                    content.AppendLine("  <ItemGroup>");
                    content.AppendLine($"    <ProjectReference Include=\"..\\{projectName.Replace(".Application.Contracts", ".Domain.Shared")}.csproj\" />");
                    content.AppendLine("    <PackageReference Include=\"Volo.Abp.Authorization\" Version=\"8.0.0\" />");
                    content.AppendLine("  </ItemGroup>");
                    break;
                case "Application":
                    content.AppendLine("  <ItemGroup>");
                    content.AppendLine($"    <ProjectReference Include=\"..\\{projectName.Replace(".Application", ".Application.Contracts")}.csproj\" />");
                    content.AppendLine($"    <ProjectReference Include=\"..\\{projectName.Replace(".Application", ".Domain")}.csproj\" />");
                    content.AppendLine("    <PackageReference Include=\"Volo.Abp.Ddd\" Version=\"8.0.0\" />");
                    content.AppendLine("  </ItemGroup>");
                    break;
                case "EntityFrameworkCore":
                    content.AppendLine("  <ItemGroup>");
                    content.AppendLine($"    <ProjectReference Include=\"..\\{projectName.Replace(".EntityFrameworkCore", ".Domain")}.csproj\" />");
                    content.AppendLine("    <PackageReference Include=\"Volo.Abp.EntityFrameworkCore\" Version=\"8.0.0\" />");
                    content.AppendLine("  </ItemGroup>");
                    break;
                case "HttpApi":
                    content.AppendLine("  <ItemGroup>");
                    content.AppendLine($"    <ProjectReference Include=\"..\\{projectName.Replace(".HttpApi", ".Application")}.csproj\" />");
                    content.AppendLine("    <PackageReference Include=\"Volo.Abp.AspNetCore.Mvc\" Version=\"8.0.0\" />");
                    content.AppendLine("  </ItemGroup>");
                    break;
                case "HttpApiClient":
                    content.AppendLine("  <ItemGroup>");
                    content.AppendLine($"    <ProjectReference Include=\"..\\{projectName.Replace(".HttpApi.Client", ".Application.Contracts")}.csproj\" />");
                    content.AppendLine("    <PackageReference Include=\"Volo.Abp.Http.Client\" Version=\"8.0.0\" />");
                    content.AppendLine("  </ItemGroup>");
                    break;
                case "Tests":
                    content.AppendLine("  <ItemGroup>");
                    content.AppendLine($"    <ProjectReference Include=\"..\\{projectName.Replace(".Tests", ".Application")}.csproj\" />");
                    content.AppendLine("    <PackageReference Include=\"Microsoft.NET.Test.Sdk\" Version=\"17.0.0\" />");
                    content.AppendLine("    <PackageReference Include=\"xunit\" Version=\"2.4.1\" />");
                    content.AppendLine("  </ItemGroup>");
                    break;
            }

            content.AppendLine("</Project>");
            return content.ToString();
        }

        private string GenerateHostProjectContent(string projectName, MicroserviceRequest microservice, SolutionGenerationRequest request)
        {
            var content = new StringBuilder();
            content.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk.Web\">");
            content.AppendLine("  <PropertyGroup>");
            content.AppendLine($"    <TargetFramework>net8.0</TargetFramework>");
            content.AppendLine($"    <RootNamespace>{projectName}</RootNamespace>");
            content.AppendLine("    <OutputType>Exe</OutputType>");
            content.AppendLine("  </PropertyGroup>");
            content.AppendLine("  <ItemGroup>");
            content.AppendLine($"    <ProjectReference Include=\"..\\{projectName.Replace(".HttpApi.Host", ".HttpApi")}.csproj\" />");
            content.AppendLine($"    <ProjectReference Include=\"..\\{projectName.Replace(".HttpApi.Host", ".EntityFrameworkCore")}.csproj\" />");
            content.AppendLine("    <PackageReference Include=\"Volo.Abp.AspNetCore.Mvc\" Version=\"8.0.0\" />");
            content.AppendLine("  </ItemGroup>");
            content.AppendLine("</Project>");
            return content.ToString();
        }

        private async Task GenerateHostFiles(string subFolderPath, string projectName, SolutionGenerationRequest request, MicroserviceRequest microservice)
        {
            var programCsPath = Path.Combine(subFolderPath, "Program.cs");
            var programContent = new StringBuilder();
            programContent.AppendLine("using Microsoft.AspNetCore.Builder;");
            programContent.AppendLine("using Volo.Abp.AspNetCore;");
            programContent.AppendLine("using Volo.Abp.AspNetCore.Mvc;");
            programContent.AppendLine();
            programContent.AppendLine($"namespace {projectName}");
            programContent.AppendLine("{");
            programContent.AppendLine("    public class Program");
            programContent.AppendLine("    {");
            programContent.AppendLine("        public static void Main(string[] args)");
            programContent.AppendLine("        {");
            programContent.AppendLine("            var builder = WebApplication.CreateBuilder(args);");
            programContent.AppendLine($"            builder.Services.AddApplication<{projectName.Replace(".HttpApi.Host", ".HttpApi") + "Module"}>();");
            programContent.AppendLine("            var app = builder.Build();");
            programContent.AppendLine("            app.InitializeApplication();");
            programContent.AppendLine("            app.Run();");
            programContent.AppendLine("        }");
            programContent.AppendLine("    }");
            programContent.AppendLine("}");

            await File.WriteAllTextAsync(programCsPath, programContent.ToString());
            Logger.LogInformation($"Created host file: {programCsPath}");
        }

        private async Task GenerateBasicProjectFiles(string subFolderPath, string projectName, SolutionGenerationRequest request, MicroserviceRequest microservice)
        {
            var classFilePath = Path.Combine(subFolderPath, "Placeholder.cs");
            var classContent = new StringBuilder();
            classContent.AppendLine($"namespace {projectName}");
            classContent.AppendLine("{");
            classContent.AppendLine("    public class Placeholder");
            classContent.AppendLine("    {");
            classContent.AppendLine("        // Placeholder class for project structure");
            classContent.AppendLine("    }");
            classContent.AppendLine("}");

            await File.WriteAllTextAsync(classFilePath, classContent.ToString());
            Logger.LogInformation($"Created placeholder file: {classFilePath}");
        }

        private List<string> GetGeneratedFilesList(string solutionPath)
        {
            var files = Directory.GetFiles(solutionPath, "*.*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(solutionPath, f))
                .ToList();
            return files;
        }




        #region gateways and apps and shared 
        private async Task GenerateRootFolderProjectsAsync(string rootPath, SolutionGenerationRequest request)
        {
            Logger.LogInformation("Generating root-level projects...");

            var rootProjects = new[]
            {
        new {
            Type = "gateway",
            Folder = Path.Combine("gateways", "web"),
            ProjectName = $"{request.CompanyName}.WebGateway",
            Template = "web"
        },
        new {
            Type = "auth",
            Folder = Path.Combine("apps", "auth-server"),
            ProjectName = $"{request.CompanyName}.AuthServer",
            Template = "web"
        },
        new {
            Type = "blazor",
            Folder = Path.Combine("apps", "blazor"),
            ProjectName = $"{request.CompanyName}.Blazor",
            Template = "blazorserver"
        },
        new {
            Type = "shared",
            Folder = "shared",
            ProjectName = $"{request.CompanyName}.DbMigrator",
            Template = "console"
        }
    };

            var mainSolutionPath = Path.Combine(rootPath, $"{request.SolutionName}.sln");

            foreach (var item in rootProjects)
            {
                var folderPath = Path.Combine(rootPath, item.Folder);
                var srcPath = Path.Combine(folderPath, "src");
                var projectPath = Path.Combine(srcPath, item.ProjectName);
                var csprojPath = Path.Combine(projectPath, $"{item.ProjectName}.csproj");
                var slnPath = Path.Combine(folderPath, $"{item.ProjectName}.sln");

                Directory.CreateDirectory(projectPath);

                // 1. Generate the project
                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"new {item.Template} -n {item.ProjectName} -o \"{projectPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    await process.StandardOutput.ReadToEndAsync();
                    await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                }

                Logger.LogInformation($"✅ Created project {item.ProjectName} using template '{item.Template}'");

                // 2. Create sub solution (.sln) in its folder
                await CreateMainSolutionAsync(slnPath);
                await AddProjectToSolutionAsync(slnPath, "src", csprojPath);

                // 3. Add to the main solution under correct solution folder
                var folderInMainSln = item.Folder.Replace("\\", "/");
                await AddProjectToSolutionAsync(mainSolutionPath, folderInMainSln, csprojPath);
            }
        }
        private async Task RunDotNetNewAsync(string template, string projectName, string outputPath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"new {template} -n {projectName} -o \"{outputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                Logger.LogError($"Failed to create {template} project '{projectName}': {error}");
            }
            else
            {
                Logger.LogInformation($"Created {template} project '{projectName}' at '{outputPath}'");
                Logger.LogInformation(output);
            }
        }

        #endregion







        private async Task GenerateSharedFilesAsync(string solutionPath, SolutionGenerationRequest request)
        {
            var sharedPath = Path.Combine(solutionPath, "shared");
            Directory.CreateDirectory(sharedPath);
            Logger.LogInformation($"Created or verified shared folder: {sharedPath}");

            var dockerPath = Path.Combine(solutionPath, "docker");
            Directory.CreateDirectory(dockerPath);
            Logger.LogInformation($"Created or verified docker folder: {dockerPath}");

            var readmePath = Path.Combine(solutionPath, "README.md");
            await File.WriteAllTextAsync(readmePath, GenerateReadmeContent(request));
            Logger.LogInformation($"Created README.md: {readmePath}");

            var dockerComposePath = Path.Combine(dockerPath, "docker-compose.yml");
            await File.WriteAllTextAsync(dockerComposePath, GenerateDockerComposeContent(request));
            Logger.LogInformation($"Created docker-compose.yml: {dockerComposePath}");

            var dockerOverridePath = Path.Combine(dockerPath, "docker-compose.override.yml");
            await File.WriteAllTextAsync(dockerOverridePath, GenerateDockerOverrideContent(request));
            Logger.LogInformation($"Created docker-compose.override.yml: {dockerOverridePath}");

            var gitignorePath = Path.Combine(solutionPath, ".gitignore");
            await File.WriteAllTextAsync(gitignorePath, GenerateGitignoreContent());
            Logger.LogInformation($"Created .gitignore: {gitignorePath}");

            await GenerateGatewaysAsync(solutionPath, request);
            await GenerateAppsAsync(solutionPath, request);
            await GenerateAuthServerAsync(solutionPath, request);
            await GenerateSharedProjectsAsync(solutionPath, request);
            //  await GenerateBuildScriptsAsync(solutionPath, request);

            if (request.SharedInfrastructure?.EnableKubernetes == true)
            {
                await GenerateKubernetesManifestsAsync(solutionPath, request);
            }
        }











































        public async Task<SolutionGenerationResponse> GetGenerationStatusAsync(Guid id)
        {
            await Task.CompletedTask;

            Logger.LogInformation("Looking for generation with ID: {Id}. Cache contains {Count} items.", id, _generationCache.Count);

            if (_generationCache.TryGetValue(id, out var response))
            {
                Logger.LogInformation("Found generation with ID: {Id}, Status: {Status}", id, response.Status);
                return response;
            }

            Logger.LogWarning("Generation with ID {Id} not found in cache. Available IDs: {AvailableIds}",
                id, string.Join(", ", _generationCache.Keys));

            throw new InvalidOperationException($"Generation with ID {id} not found");
        }

        public async Task<byte[]> DownloadSolutionAsync(Guid id)
        {
            if (!_generationCache.TryGetValue(id, out var response))
            {
                throw new InvalidOperationException($"Generation with ID {id} not found");
            }

            if (!response.IsCompleted)
            {
                throw new InvalidOperationException("Solution generation is not completed yet");
            }

            // البحث عن ملف ZIP المولد
            var zipFiles = Directory.GetFiles(_outputPath, "*.zip")
                .Where(f => f.Contains(response.SolutionName))
                .OrderByDescending(f => File.GetCreationTime(f))
                .FirstOrDefault();

            if (zipFiles != null && File.Exists(zipFiles))
            {
                return await File.ReadAllBytesAsync(zipFiles);
            }

            // إذا لم يوجد ملف ZIP، إنشاء ملف نصي بسيط
            var content = $"# {response.SolutionName}\n\nGenerated solution files:\n{string.Join("\n", response.GeneratedFiles)}\n\nGenerated at: {response.CreatedAt}\nCompleted at: {response.CompletedAt}";
            return System.Text.Encoding.UTF8.GetBytes(content);
        }

        public async Task<bool> ValidateSolutionNameAsync(string solutionName)
        {
            await Task.CompletedTask;

            if (string.IsNullOrWhiteSpace(solutionName))
                return false;

            // Check if it's a valid C# namespace
            var namespacePattern = @"^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)*$";
            return Regex.IsMatch(solutionName, namespacePattern);
        }

        public async Task<bool> ValidateMicroserviceNameAsync(string microserviceName)
        {
            await Task.CompletedTask;

            if (string.IsNullOrWhiteSpace(microserviceName))
                return false;

            // Check if it's a valid C# identifier
            var identifierPattern = @"^[a-zA-Z_][a-zA-Z0-9_]*$";
            return Regex.IsMatch(microserviceName, identifierPattern);
        }

        public async Task<int> GetNextAvailablePortAsync()
        {
            await Task.CompletedTask;

            // Start from 44305 and find next available port
            // Note: This method doesn't track ports from current session
            // The client should pass used ports for better tracking
            var usedPorts = new HashSet<int> { 44300, 44301, 44302, 44303, 44304 }; // Common ABP ports

            for (int port = 44305; port <= 65535; port++)
            {
                if (!usedPorts.Contains(port))
                {
                    return port;
                }
            }

            return 44305; // Fallback
        }

        public async Task<int> GetNextAvailablePortWithExclusionsAsync(List<int> usedPorts)
        {
            await Task.CompletedTask;

            // Combine common ABP ports with user-provided used ports
            var allUsedPorts = new HashSet<int> { 44300, 44301, 44302, 44303, 44304 };
            foreach (var port in usedPorts)
            {
                allUsedPorts.Add(port);
            }

            for (int port = 44305; port <= 65535; port++)
            {
                if (!allUsedPorts.Contains(port))
                {
                    return port;
                }
            }

            return 44305; // Fallback
        }

        #region Helper Methods

    

    
        private async Task<string> CreateZipPackageAsync(string solutionPath, string solutionName)
        {
            var zipPath = Path.Combine(_outputPath, $"{solutionName}_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(solutionPath, zipPath);

            await Task.CompletedTask;
            return zipPath;
        }

        //private List<string> GetGeneratedFilesList(string solutionPath)
        //{
        //    var files = new List<string>();

        //    if (Directory.Exists(solutionPath))
        //    {
        //        files.AddRange(Directory.GetFiles(solutionPath, "*", SearchOption.AllDirectories)
        //            .Select(f => Path.GetRelativePath(solutionPath, f)));
        //    }

        //    return files;
        //}

        #endregion

        #region Content Generators



 








        //private string GenerateProjectFileContent(string projectName, MicroserviceRequest microservice, string projectType = "Domain.Shared")
        //{
        //    return projectType switch
        //    {
        //        "Domain.Shared" => GenerateDomainSharedProject(projectName, microservice),
        //        "Domain" => GenerateDomainProject(projectName, microservice),
        //        "Application.Contracts" => GenerateApplicationContractsProject(projectName, microservice),
        //        "Application" => GenerateApplicationProject(projectName, microservice),
        //        "EntityFrameworkCore" => GenerateEntityFrameworkCoreProject(projectName, microservice),
        //        "HttpApi" => GenerateHttpApiProject(projectName, microservice),
        //        "HttpApi.Client" => GenerateHttpApiClientProject(projectName, microservice),
        //        "HttpApi.Host" => GenerateHttpApiHostProject(projectName, microservice),
        //        _ => GenerateDomainSharedProject(projectName, microservice)
        //    };
        //}

        private string GenerateDomainSharedProject(string projectName, MicroserviceRequest microservice)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Volo.Abp.Core"" Version=""4.2.0"" />
  </ItemGroup>

</Project>";
        }

        private string GenerateDomainProject(string projectName, MicroserviceRequest microservice)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Volo.Abp.Ddd.Domain"" Version=""4.2.0"" />
    <PackageReference Include=""Volo.Abp.EntityFrameworkCore"" Version=""4.2.0"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\{microservice.Name}.Domain.Shared\{microservice.Name}.Domain.Shared.csproj"" />
  </ItemGroup>

</Project>";
        }

        private string GenerateApplicationContractsProject(string projectName, MicroserviceRequest microservice)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Volo.Abp.Ddd.Application.Contracts"" Version=""4.2.0"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\{microservice.Name}.Domain.Shared\{microservice.Name}.Domain.Shared.csproj"" />
  </ItemGroup>

</Project>";
        }

        private string GenerateApplicationProject(string projectName, MicroserviceRequest microservice)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Volo.Abp.Ddd.Application"" Version=""4.2.0"" />
    <PackageReference Include=""Volo.Abp.AutoMapper"" Version=""4.2.0"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\{microservice.Name}.Application.Contracts\{microservice.Name}.Application.Contracts.csproj"" />
    <ProjectReference Include=""..\{microservice.Name}.Domain\{microservice.Name}.Domain.csproj"" />
  </ItemGroup>

</Project>";
        }

        private string GenerateEntityFrameworkCoreProject(string projectName, MicroserviceRequest microservice)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Volo.Abp.EntityFrameworkCore.SqlServer"" Version=""4.2.0"" />
    <PackageReference Include=""Microsoft.EntityFrameworkCore.Tools"" Version=""9.0.0"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\{microservice.Name}.Domain\{microservice.Name}.Domain.csproj"" />
  </ItemGroup>

</Project>";
        }

        private string GenerateHttpApiProject(string projectName, MicroserviceRequest microservice)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Volo.Abp.AspNetCore.Mvc"" Version=""4.2.0"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\{microservice.Name}.Application.Contracts\{microservice.Name}.Application.Contracts.csproj"" />
  </ItemGroup>

</Project>";
        }

        private string GenerateHttpApiHostProject(string projectName, MicroserviceRequest microservice)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Volo.Abp.AspNetCore.Mvc"" Version=""4.2.0"" />
    <PackageReference Include=""Volo.Abp.Autofac"" Version=""4.2.0"" />
    <PackageReference Include=""Volo.Abp.EntityFrameworkCore.SqlServer"" Version=""4.2.0"" />
    <PackageReference Include=""Serilog.AspNetCore"" Version=""8.0.0"" />
    <PackageReference Include=""Serilog.Sinks.File"" Version=""5.0.0"" />
    <PackageReference Include=""Serilog.Sinks.Console"" Version=""5.0.0"" />
    <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.5.0"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\{microservice.Name}.Application\{microservice.Name}.Application.csproj"" />
    <ProjectReference Include=""..\{microservice.Name}.EntityFrameworkCore\{microservice.Name}.EntityFrameworkCore.csproj"" />
    <PackageReference Include=""..\{microservice.Name}.HttpApi\{microservice.Name}.HttpApi.csproj"" />
  </ItemGroup>

</Project>";
        }

        //private string GetProjectTypeFromName(string projectName)
        //{
        //    if (projectName.EndsWith(".Domain.Shared"))
        //        return "Domain.Shared";
        //    if (projectName.EndsWith(".Domain"))
        //        return "Domain";
        //    if (projectName.EndsWith(".Application.Contracts"))
        //        return "Application.Contracts";
        //    if (projectName.EndsWith(".Application"))
        //        return "Application";
        //    if (projectName.EndsWith(".EntityFrameworkCore"))
        //        return "EntityFrameworkCore";
        //    if (projectName.EndsWith(".HttpApi.Client"))
        //        return "HttpApi.Client";
        //    if (projectName.EndsWith(".HttpApi.Host"))
        //        return "HttpApi.Host";
        //    if (projectName.EndsWith(".HttpApi"))
        //        return "HttpApi";

        //    return "Domain.Shared"; // Default
        //}

        private string GenerateHttpApiClientProject(string projectName, MicroserviceRequest microservice)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Volo.Abp.Http.Client"" Version=""4.2.0"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\{microservice.Name}.Application.Contracts\{microservice.Name}.Application.Contracts.csproj"" />
  </ItemGroup>

</Project>";
        }

        private string GenerateTestProjectContent(string projectName, MicroserviceRequest microservice, string testType)
        {
            return testType switch
            {
                "Domain.Tests" => GenerateDomainTestProject(projectName, microservice),
                "Application.Tests" => GenerateApplicationTestProject(projectName, microservice),
                _ => GenerateDomainTestProject(projectName, microservice)
            };
        }

        private string GenerateDomainTestProject(string projectName, MicroserviceRequest microservice)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.8.0"" />
    <PackageReference Include=""xunit"" Version=""2.6.1"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.5.3"" />
    <PackageReference Include=""Volo.Abp.TestBase"" Version=""4.2.0"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\..\src\{microservice.Name}.Domain\{microservice.Name}.Domain.csproj"" />
  </ItemGroup>

</Project>";
        }

        private string GenerateApplicationTestProject(string projectName, MicroserviceRequest microservice)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.8.0"" />
    <PackageReference Include=""xunit"" Version=""2.6.1"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.5.3"" />
    <PackageReference Include=""Volo.Abp.TestBase"" Version=""4.2.0"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\..\src\{microservice.Name}.Application\{microservice.Name}.Application.csproj"" />
    <ProjectReference Include=""..\..\src\{microservice.Name}.EntityFrameworkCore\{microservice.Name}.EntityFrameworkCore.csproj"" />
  </ItemGroup>

</Project>";
        }

        private string GetThemePackage(AppTheme theme, string appType)
        {
            return theme switch
            {
                AppTheme.LeptonXLite => appType switch
                {
                    "WebAssembly" => @"<PackageReference Include=""Volo.Abp.AspNetCore.Components.WebAssembly.LeptonXLiteTheme"" Version=""4.2.0"" />",
                    "Server" => @"<PackageReference Include=""Volo.Abp.AspNetCore.Components.Server.LeptonXLiteTheme"" Version=""4.2.0"" />",
                    "Mvc" => @"<PackageReference Include=""Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite"" Version=""4.2.0"" />",
                    _ => @"<PackageReference Include=""Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite"" Version=""4.2.0"" />"
                },
                AppTheme.LeptonX => appType switch
                {
                    "WebAssembly" => @"<PackageReference Include=""Volo.Abp.AspNetCore.Components.WebAssembly.LeptonXTheme"" Version=""4.2.0"" />",
                    "Server" => @"<PackageReference Include=""Volo.Abp.AspNetCore.Components.Server.LeptonXTheme"" Version=""4.2.0"" />",
                    "Mvc" => @"<PackageReference Include=""Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonX"" Version=""4.2.0"" />",
                    _ => @"<PackageReference Include=""Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonX"" Version=""4.2.0"" />"
                },
                AppTheme.Basic => appType switch
                {
                    "WebAssembly" => @"<PackageReference Include=""Volo.Abp.AspNetCore.Components.WebAssembly.BasicTheme"" Version=""4.2.0"" />",
                    "Server" => @"<PackageReference Include=""Volo.Abp.AspNetCore.Components.Server.BasicTheme"" Version=""4.2.0"" />",
                    "Mvc" => @"<PackageReference Include=""Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic"" Version=""4.2.0"" />",
                    _ => @"<PackageReference Include=""Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic"" Version=""4.2.0"" />"
                },
                AppTheme.Lepton => appType switch
                {
                    "WebAssembly" => @"<PackageReference Include=""Volo.Abp.AspNetCore.Components.WebAssembly.LeptonTheme"" Version=""4.2.0"" />",
                    "Server" => @"<PackageReference Include=""Volo.Abp.AspNetCore.Components.Server.LeptonTheme"" Version=""4.2.0"" />",
                    "Mvc" => @"<PackageReference Include=""Volo.Abp.AspNetCore.Mvc.UI.Theme.Lepton"" Version=""4.2.0"" />",
                    _ => @"<PackageReference Include=""Volo.Abp.AspNetCore.Mvc.UI.Theme.Lepton"" Version=""4.2.0"" />"
                },
                _ => @"<PackageReference Include=""Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite"" Version=""4.2.0"" />"
            };
        }

        private string GenerateAngularPackageJson(string projectName, AppRequest app, SolutionGenerationRequest request)
        {
            return $@"{{
  ""name"": ""{projectName.ToLower()}"",
  ""version"": ""1.0.0"",
  ""scripts"": {{
    ""ng"": ""ng"",
    ""start"": ""ng serve --port {app.Port}"",
    ""build"": ""ng build"",
    ""watch"": ""ng build --watch --configuration development"",
    ""test"": ""ng test""
  }},
  ""dependencies"": {{
    ""@angular/animations"": ""^17.0.0"",
    ""@angular/common"": ""^17.0.0"",
    ""@angular/compiler"": ""^17.0.0"",
    ""@angular/core"": ""^17.0.0"",
    ""@angular/forms"": ""^17.0.0"",
    ""@angular/platform-browser"": ""^17.0.0"",
    ""@angular/platform-browser-dynamic"": ""^17.0.0"",
    ""@angular/router"": ""^17.0.0"",
    ""@abp/ng.core"": ""^4.2.0"",
    ""@abp/ng.theme.lepton-x"": ""^4.2.0"",
    ""rxjs"": ""~7.8.0"",
    ""tslib"": ""^2.3.0"",
    ""zone.js"": ""~0.14.0""
  }},
  ""devDependencies"": {{
    ""@angular-devkit/build-angular"": ""^17.0.0"",
    ""@angular/cli"": ""^17.0.0"",
    ""@angular/compiler-cli"": ""^17.0.0"",
    ""@types/jasmine"": ""~5.1.0"",
    ""jasmine-core"": ""~5.1.0"",
    ""karma"": ""~6.4.0"",
    ""karma-chrome-launcher"": ""~3.2.0"",
    ""karma-coverage"": ""~2.2.0"",
    ""karma-jasmine"": ""~5.1.0"",
    ""karma-jasmine-html-reporter"": ""~2.1.0"",
    ""typescript"": ""~5.2.0""
  }}
}}";
        }

        private string GenerateReactPackageJson(string projectName, AppRequest app, SolutionGenerationRequest request)
        {
            return $@"{{
  ""name"": ""{projectName.ToLower()}"",
  ""version"": ""1.0.0"",
  ""private"": true,
  ""scripts"": {{
    ""start"": ""react-scripts start"",
    ""build"": ""react-scripts build"",
    ""test"": ""react-scripts test"",
    ""eject"": ""react-scripts eject""
  }},
  ""dependencies"": {{
    ""react"": ""^18.2.0"",
    ""react-dom"": ""^18.2.0"",
    ""react-router-dom"": ""^6.8.0"",
    ""@abp/react"": ""^4.2.0"",
    ""axios"": ""^1.3.0"",
    ""antd"": ""^5.0.0""
  }},
  ""devDependencies"": {{
    ""@types/react"": ""^18.0.0"",
    ""@types/react-dom"": ""^18.0.0"",
    ""react-scripts"": ""5.0.1"",
    ""typescript"": ""^4.9.0""
  }},
  ""browserslist"": {{
    ""production"": [
      "">0.2%"",
      ""not dead"",
      ""not op_mini all""
    ],
    ""development"": [
      ""last 1 chrome version"",
      ""last 1 firefox version"",
      ""last 1 safari version""
    ]
  }}
}}";
        }

        private string GenerateReadmeContent(SolutionGenerationRequest request)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {request.SolutionName}");
            sb.AppendLine();
            sb.AppendLine($"**Company:** {request.CompanyName}");
            sb.AppendLine($"**Description:** {request.Description}");
            sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**Framework:** ABP Framework Microservices");
            sb.AppendLine();

            sb.AppendLine("## Architecture Overview");
            sb.AppendLine();
            sb.AppendLine("This solution follows ABP Framework microservices architecture with the following structure:");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine("├── services/           # Microservices");
            sb.AppendLine("├── gateways/          # API Gateways");
            sb.AppendLine("├── apps/              # Client Applications");
            sb.AppendLine("├── shared/            # Shared Libraries");
            sb.AppendLine("├── docker/            # Docker Configurations");
            sb.AppendLine("├── k8s/               # Kubernetes Manifests");
            sb.AppendLine("├── build/             # Build Scripts");
            sb.AppendLine("└── docs/              # Documentation");
            sb.AppendLine("```");
            sb.AppendLine();

            // Services section
            sb.AppendLine("## Services");
            sb.AppendLine();
            foreach (var microservice in request.Microservices)
            {
                sb.AppendLine($"### {microservice.Name}");
                sb.AppendLine($"- **Port:** {microservice.Port}");
                sb.AppendLine($"- **Description:** {microservice.Description}");
                sb.AppendLine($"- **API Enabled:** {microservice.EnableApi}");
                sb.AppendLine($"- **gRPC Enabled:** {microservice.EnableGrpc}");
                sb.AppendLine();
            }

            // Auth Service
            sb.AppendLine($"### {request.AuthService.Name} (Authentication)");
            sb.AppendLine($"- **Port:** {request.AuthService.Port}");
            sb.AppendLine($"- **Description:** {request.AuthService.Description}");
            sb.AppendLine($"- **Provider:** {request.AuthService.Provider}");
            sb.AppendLine();

            // Gateways section
            sb.AppendLine("## API Gateways");
            sb.AppendLine();
            foreach (var gateway in request.Gateways)
            {
                sb.AppendLine($"### {gateway.Name}");
                sb.AppendLine($"- **Port:** {gateway.Port}");
                sb.AppendLine($"- **Type:** {gateway.Type}");
                sb.AppendLine($"- **Description:** {gateway.Description}");
                sb.AppendLine();
            }

            // Apps section
            sb.AppendLine("## Client Applications");
            sb.AppendLine();
            foreach (var app in request.Apps)
            {
                sb.AppendLine($"### {app.Name}");
                sb.AppendLine($"- **Port:** {app.Port}");
                sb.AppendLine($"- **Type:** {app.Type}");
                sb.AppendLine($"- **Theme:** {app.Theme}");
                sb.AppendLine($"- **Description:** {app.Description}");
                sb.AppendLine();
            }

            // Infrastructure section
            sb.AppendLine("## Infrastructure");
            sb.AppendLine();
            if (request.SharedInfrastructure.EnableRedis)
                sb.AppendLine("- **Redis:** Enabled for caching and distributed locking");
            if (request.SharedInfrastructure.EnableRabbitMQ)
                sb.AppendLine("- **RabbitMQ:** Enabled for message queuing");
            if (request.SharedInfrastructure.EnableElasticsearch)
                sb.AppendLine("- **Elasticsearch:** Enabled for logging and search");
            if (request.SharedInfrastructure.EnableDocker)
                sb.AppendLine("- **Docker:** Enabled with docker-compose");
            if (request.SharedInfrastructure.EnableKubernetes)
                sb.AppendLine("- **Kubernetes:** Enabled with manifests");
            sb.AppendLine();

            // Getting Started section
            sb.AppendLine("## Getting Started");
            sb.AppendLine();
            sb.AppendLine("### Prerequisites");
            sb.AppendLine("- .NET 9.0 SDK");
            sb.AppendLine("- Docker Desktop");
            if (request.SharedInfrastructure.EnableRedis)
                sb.AppendLine("- Redis");
            if (request.SharedInfrastructure.EnableRabbitMQ)
                sb.AppendLine("- RabbitMQ");
            sb.AppendLine();

            sb.AppendLine("### Running the Solution");
            sb.AppendLine("1. Clone the repository");
            sb.AppendLine("2. Navigate to the docker folder");
            sb.AppendLine("3. Run `docker-compose up -d` to start infrastructure services");
            sb.AppendLine("4. Run the build script from the build folder");
            sb.AppendLine("5. Start the services in the following order:");
            sb.AppendLine($"   - {request.AuthService.Name} (Authentication)");
            foreach (var service in request.Microservices)
            {
                sb.AppendLine($"   - {service.Name}");
            }
            foreach (var gateway in request.Gateways)
            {
                sb.AppendLine($"   - {gateway.Name}");
            }
            foreach (var app in request.Apps)
            {
                sb.AppendLine($"   - {app.Name}");
            }
            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateDockerComposeContent(SolutionGenerationRequest request)
        {
            var sb = new StringBuilder();
            sb.AppendLine("version: '3.8'");
            sb.AppendLine();
            sb.AppendLine("services:");

            foreach (var microservice in request.Microservices)
            {
                sb.AppendLine($"  {microservice.Name.ToLower()}:");
                sb.AppendLine($"    build: ./src/{microservice.Name}");
                sb.AppendLine($"    ports:");
                sb.AppendLine($"      - \"{microservice.Port}:{microservice.Port}\"");
                sb.AppendLine($"    environment:");
                sb.AppendLine($"      - ASPNETCORE_ENVIRONMENT=Development");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string GenerateGitignoreContent()
        {
            return @"## Ignore Visual Studio temporary files, build results, and
## files generated by popular Visual Studio add-ons.

# User-specific files
*.suo
*.user
*.userosscache
*.sln.docstates

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
build/
bld/
[Bb]in/
[Oo]bj/

# Visual Studio 2015 cache/options directory
.vs/

# MSTest test Results
[Tt]est[Rr]esult*/
[Bb]uild[Ll]og.*

# NUnit
*.VisualState.xml
TestResult.xml

# .NET Core
project.lock.json
project.fragment.lock.json
artifacts/
**/Properties/launchSettings.json

# Logs
logs
*.log
npm-debug.log*
yarn-debug.log*
yarn-error.log*

# Runtime data
pids
*.pid
*.seed
*.pid.lock

# Coverage directory used by tools like istanbul
coverage

# nyc test coverage
.nyc_output

# node_modules
node_modules/
";
        }

        private async Task GenerateGatewaysAsync(string solutionPath, SolutionGenerationRequest request)
        {
            foreach (var gateway in request.Gateways)
            {
                var gatewayPath = Path.Combine(solutionPath, "gateways", gateway.Name);
                var projectName = $"{request.CompanyName}.{gateway.Name}";

                // Create gateway project
                var gatewayContent = GenerateGatewayProjectContent(projectName, gateway, request);
                await File.WriteAllTextAsync(Path.Combine(gatewayPath, $"{projectName}.csproj"), gatewayContent);

                // Create gateway configuration files
                await GenerateGatewayConfigFiles(gatewayPath, projectName, gateway, request);
            }
        }

        private async Task GenerateAppsAsync(string solutionPath, SolutionGenerationRequest request)
        {
            foreach (var app in request.Apps)
            {
                var appPath = Path.Combine(solutionPath, "apps", app.Name);
                var projectName = $"{request.CompanyName}.{app.Name}";

                // Create app project based on type
                var appContent = GenerateAppProjectContent(projectName, app, request);
                await File.WriteAllTextAsync(Path.Combine(appPath, $"{projectName}.csproj"), appContent);

                // Create app files
                await GenerateAppFiles(appPath, projectName, app, request);
            }
        }

        private async Task GenerateAuthServerAsync(string solutionPath, SolutionGenerationRequest request)
        {
            var authPath = Path.Combine(solutionPath, "services", request.AuthService.Name);
            var projectName = $"{request.CompanyName}.{request.AuthService.Name}";

            // Create auth server project
            var authContent = GenerateAuthServerProjectContent(projectName, request.AuthService, request);
            await File.WriteAllTextAsync(Path.Combine(authPath, $"{projectName}.csproj"), authContent);

            // Create auth server files
            await GenerateAuthServerFiles(authPath, projectName, request.AuthService, request);
        }

        private async Task GenerateSharedProjectsAsync(string solutionPath, SolutionGenerationRequest request)
        {
            var sharedPath = Path.Combine(solutionPath, "shared");
            Directory.CreateDirectory(sharedPath);
            Logger.LogInformation("Created shared directory: {SharedPath}", sharedPath);

            // Shared projects
            var sharedProjects = new[]
            {
        $"{request.CompanyName}.Shared",
        $"{request.CompanyName}.Shared.Hosting",
        $"{request.CompanyName}.Shared.Localization"
    };

            foreach (var project in sharedProjects)
            {
                var projectPath = Path.Combine(sharedPath, project);
                Directory.CreateDirectory(projectPath);
                Logger.LogInformation("Created shared project folder: {ProjectPath}", projectPath);

                // Generate .csproj file
                var projectContent = project switch
                {
                    var p when p.EndsWith(".Shared") => GenerateSharedContent(p, request),
                    var p when p.EndsWith(".Shared.Hosting") => GenerateSharedHostingContent(p, request),
                    var p when p.EndsWith(".Shared.Localization") => GenerateSharedLocalizationContent(p, request),
                    _ => throw new ArgumentException($"Unknown shared project type: {project}")
                };
                var projectFilePath = Path.Combine(projectPath, $"{project}.csproj");
                await File.WriteAllTextAsync(projectFilePath, projectContent);
                Logger.LogInformation("Created shared project file: {ProjectFilePath}", projectFilePath);
            }
        }
        private string GenerateSharedContent(string projectName, SolutionGenerationRequest request)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Volo.Abp.Core"" Version=""4.2.0"" />
  </ItemGroup>
</Project>";
        }

        private string GenerateSharedHostingContent(string projectName, SolutionGenerationRequest request)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Volo.Abp.AspNetCore.Serilog"" Version=""4.2.0"" />
    <ProjectReference Include=""..\\{request.CompanyName}.Shared\\{request.CompanyName}.Shared.csproj"" />
  </ItemGroup>
</Project>";
        }

        private string GenerateSharedLocalizationContent(string projectName, SolutionGenerationRequest request)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Volo.Abp.Localization"" Version=""4.2.0"" />
    <ProjectReference Include=""..\\{request.CompanyName}.Shared\\{request.CompanyName}.Shared.csproj"" />
  </ItemGroup>
</Project>";
        }

        // Placeholder methods for content generation
        //private async Task GenerateBasicProjectFiles(string projectPath, string projectName, SolutionGenerationRequest request, MicroserviceRequest microservice)
        //{
        //    // Create basic class files based on project type
        //    await Task.CompletedTask;
        //}

        //private async Task GenerateHostFiles(string hostPath, string projectName, SolutionGenerationRequest request, MicroserviceRequest microservice)
        //{
        //    // Create Program.cs
        //    var programContent = GenerateProgramCs(projectName, request, microservice);
        //    await File.WriteAllTextAsync(Path.Combine(hostPath, "Program.cs"), programContent);

        //    // Create appsettings.json
        //    var appSettingsContent = GenerateAppSettings(projectName, request, microservice);
        //    await File.WriteAllTextAsync(Path.Combine(hostPath, "appsettings.json"), appSettingsContent);

        //    // Create appsettings.Development.json
        //    var devAppSettingsContent = GenerateDevAppSettings(projectName, request, microservice);
        //    await File.WriteAllTextAsync(Path.Combine(hostPath, "appsettings.Development.json"), devAppSettingsContent);

        //    // Create launchSettings.json
        //    var launchSettingsContent = GenerateLaunchSettings(projectName, request, microservice);
        //    var propertiesPath = Path.Combine(hostPath, "Properties");
        //    Directory.CreateDirectory(propertiesPath);
        //    await File.WriteAllTextAsync(Path.Combine(propertiesPath, "launchSettings.json"), launchSettingsContent);
        //}

        private async Task GenerateGatewayConfigFiles(string gatewayPath, string projectName, GatewayRequest gateway, SolutionGenerationRequest request)
        {
            // Create gateway configuration files
            await Task.CompletedTask;
        }

        private async Task GenerateAppFiles(string appPath, string projectName, AppRequest app, SolutionGenerationRequest request)
        {
            // Create app files based on app type
            await Task.CompletedTask;
        }

        private async Task GenerateAuthServerFiles(string authPath, string projectName, AuthServiceRequest authService, SolutionGenerationRequest request)
        {
            // Create auth server files
            await Task.CompletedTask;
        }

        // Content generation methods
        //private string GenerateHostProjectContent(string projectName, MicroserviceRequest microservice, SolutionGenerationRequest request)
        //{
        //    return GenerateProjectFileContent(projectName, microservice);
        //}

        private string GenerateDockerOverrideContent(SolutionGenerationRequest request)
        {
            return "# Docker override configuration\nversion: '3.8'\nservices:\n  # Override services here";
        }

        private string GenerateGatewayProjectContent(string projectName, GatewayRequest gateway, SolutionGenerationRequest request)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Ocelot"" Version=""23.2.2"" />
    <PackageReference Include=""Volo.Abp.AspNetCore.Mvc"" Version=""4.2.0"" />
    <PackageReference Include=""Volo.Abp.Autofac"" Version=""4.2.0"" />
    <PackageReference Include=""Serilog.AspNetCore"" Version=""8.0.0"" />
  </ItemGroup>
</Project>";
        }

        private string GenerateAppProjectContent(string projectName, AppRequest app, SolutionGenerationRequest request)
        {
            return app.Type switch
            {
                AppType.BlazorWebAssembly => GenerateBlazorWebAssemblyProject(projectName, app, request),
                AppType.BlazorServer => GenerateBlazorServerProject(projectName, app, request),
                AppType.Mvc => GenerateMvcProject(projectName, app, request),
                AppType.Angular => GenerateAngularProject(projectName, app, request),
                AppType.React => GenerateReactProject(projectName, app, request),
                AppType.Maui => GenerateMauiProject(projectName, app, request),
                _ => GenerateBlazorWebAssemblyProject(projectName, app, request)
            };
        }

        private string GenerateBlazorWebAssemblyProject(string projectName, AppRequest app, SolutionGenerationRequest request)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk.BlazorWebAssembly"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Components.WebAssembly"" Version=""9.0.4"" />
    <PackageReference Include=""Microsoft.AspNetCore.Components.WebAssembly.DevServer"" Version=""9.0.4"" PrivateAssets=""all"" />
    <PackageReference Include=""Volo.Abp.AspNetCore.Components.WebAssembly"" Version=""4.2.0"" />
    <PackageReference Include=""Volo.Abp.AspNetCore.Components.WebAssembly.Theming"" Version=""4.2.0"" />
    {GetThemePackage(app.Theme, "WebAssembly")}
  </ItemGroup>

</Project>";
        }

        private string GenerateBlazorServerProject(string projectName, AppRequest app, SolutionGenerationRequest request)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite"" Version=""4.2.0"" />
    <PackageReference Include=""Volo.Abp.AspNetCore.Components.Server.LeptonXLiteTheme"" Version=""4.2.0"" />
    <PackageReference Include=""Volo.Abp.AspNetCore.Authentication.OpenIdConnect"" Version=""4.2.0"" />
    <PackageReference Include=""Volo.Abp.Http.Client.IdentityModel.WebAssembly"" Version=""4.2.0"" />
    {GetThemePackage(app.Theme, "Server")}
  </ItemGroup>

</Project>";
        }

        private string GenerateMvcProject(string projectName, AppRequest app, SolutionGenerationRequest request)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite"" Version=""4.2.0"" />
    <PackageReference Include=""Volo.Abp.AspNetCore.Authentication.OpenIdConnect"" Version=""4.2.0"" />
    <PackageReference Include=""Volo.Abp.Http.Client.IdentityModel"" Version=""4.2.0"" />
    <PackageReference Include=""Volo.Abp.AspNetCore.Mvc.Client"" Version=""4.2.0"" />
    {GetThemePackage(app.Theme, "Mvc")}
  </ItemGroup>

</Project>";
        }

        private string GenerateAngularProject(string projectName, AppRequest app, SolutionGenerationRequest request)
        {
            // Angular projects use package.json instead of .csproj
            return GenerateAngularPackageJson(projectName, app, request);
        }

        private string GenerateReactProject(string projectName, AppRequest app, SolutionGenerationRequest request)
        {
            // React projects use package.json instead of .csproj
            return GenerateReactPackageJson(projectName, app, request);
        }

        private string GenerateMauiProject(string projectName, AppRequest app, SolutionGenerationRequest request)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFrameworks>net9.0-android;net9.0-ios;net9.0-maccatalyst;net9.0-windows10.0.19041.0</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <RootNamespace>{projectName}</RootNamespace>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <!-- Display Version Info -->
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
    <ApplicationVersion>1</ApplicationVersion>

    <SupportedOSPlatformVersion Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'"">21.0</SupportedOSPlatformVersion>
    <SupportedOSPlatformVersion Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'"">11.0</SupportedOSPlatformVersion>
    <SupportedOSPlatformVersion Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'maccatalyst'"">13.1</SupportedOSPlatformVersion>
    <SupportedOSPlatformVersion Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'"">10.0.17763.0</SupportedOSPlatformVersion>
    <TargetPlatformMinVersion Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'"">10.0.17763.0</TargetPlatformMinVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.Maui.Controls"" Version=""9.0.0"" />
    <PackageReference Include=""Microsoft.Maui.Controls.Compatibility"" Version=""9.0.0"" />
    <PackageReference Include=""Microsoft.AspNetCore.Components.WebView.Maui"" Version=""9.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging.Debug"" Version=""9.0.0"" />
    <PackageReference Include=""Volo.Abp.Http.Client.IdentityModel"" Version=""4.2.0"" />
  </ItemGroup>

</Project>";
        }

        private string GenerateAuthServerProjectContent(string projectName, AuthServiceRequest authService, SolutionGenerationRequest request)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Volo.Abp.Account.Web.OpenIddict"" Version=""4.2.0"" />
    <PackageReference Include=""Volo.Abp.OpenIddict.AspNetCore"" Version=""4.2.0"" />
    <PackageReference Include=""Volo.Abp.Autofac"" Version=""4.2.0"" />
    <PackageReference Include=""Volo.Abp.EntityFrameworkCore.SqlServer"" Version=""4.2.0"" />
    <PackageReference Include=""Serilog.AspNetCore"" Version=""8.0.0"" />
  </ItemGroup>
</Project>";
        }



        private string GenerateBuildScript(SolutionGenerationRequest request)
        {
            return $@"# Build script for {request.SolutionName}
Write-Host ""Building {request.SolutionName}...""
dotnet restore
dotnet build
Write-Host ""Build completed!""";
        }

        private string GenerateBuildShScript(SolutionGenerationRequest request)
        {
            return $@"#!/bin/bash
# Build script for {request.SolutionName}
echo ""Building {request.SolutionName}...""
dotnet restore
dotnet build
echo ""Build completed!""";
        }

        private string GenerateK8sNamespace(SolutionGenerationRequest request)
        {
            return $@"apiVersion: v1
kind: Namespace
metadata:
  name: {request.SolutionName.ToLower()}";
        }

        private string GenerateK8sServiceManifest(MicroserviceRequest service, SolutionGenerationRequest request)
        {
            return $@"apiVersion: apps/v1
kind: Deployment
metadata:
  name: {service.Name.ToLower()}
  namespace: {request.SolutionName.ToLower()}
spec:
  replicas: 1
  selector:
    matchLabels:
      app: {service.Name.ToLower()}
  template:
    metadata:
      labels:
        app: {service.Name.ToLower()}
    spec:
      containers:
      - name: {service.Name.ToLower()}
        image: {service.Name.ToLower()}:latest
        ports:
        - containerPort: {service.Port}";
        }

        private string GenerateProgramCs(string projectName, SolutionGenerationRequest request, MicroserviceRequest microservice)
        {
            return $@"using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace {projectName};

[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAutofacModule)
)]
public class {microservice.Name}Module : AbpModule
{{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {{
        // Configure services here
    }}
}}

public class Program
{{
    public static async Task<int> Main(string[] args)
    {{
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override(""Microsoft"", LogEventLevel.Information)
            .MinimumLevel.Override(""Microsoft.EntityFrameworkCore"", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File(""Logs/logs.txt""))
            .WriteTo.Async(c => c.Console())
            .CreateLogger();

        try
        {{
            Log.Information(""Starting {microservice.Name} service."");
            var builder = WebApplication.CreateBuilder(args);
            builder.Host
                .UseAutofac()
                .UseSerilog();

            await builder.AddApplicationAsync<{microservice.Name}Module>();
            var app = builder.Build();
            await app.InitializeApplicationAsync();

            app.UseRouting();
            app.MapGet(""/"", () => ""Hello from {microservice.Name} service!"");

            await app.RunAsync();
            return 0;
        }}
        catch (Exception ex)
        {{
            Log.Fatal(ex, ""Host terminated unexpectedly!"");
            return 1;
        }}
        finally
        {{
            Log.CloseAndFlush();
        }}
    }}
}}";
        }

        private string GenerateAppSettings(string projectName, SolutionGenerationRequest request, MicroserviceRequest microservice)
        {
            return $@"{{
  ""ConnectionStrings"": {{
    ""Default"": ""Server=(LocalDb)\\MSSQLLocalDB;Database={request.SolutionName}_{microservice.Name};Trusted_Connection=True;TrustServerCertificate=True""
  }},
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }}
  }},
  ""AllowedHosts"": ""*""
}}";
        }

        private string GenerateDevAppSettings(string projectName, SolutionGenerationRequest request, MicroserviceRequest microservice)
        {
            return $@"{{
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }}
  }}
}}";
        }

        private string GenerateLaunchSettings(string projectName, SolutionGenerationRequest request, MicroserviceRequest microservice)
        {
            return $@"{{
  ""profiles"": {{
    ""{projectName}"": {{
      ""commandName"": ""Project"",
      ""dotnetRunMessages"": true,
      ""launchBrowser"": true,
      ""applicationUrl"": ""https://localhost:{microservice.Port};http://localhost:{microservice.Port - 1000}"",
      ""environmentVariables"": {{
        ""ASPNETCORE_ENVIRONMENT"": ""Development""
      }}
    }}
  }}
}}";
        }

        private string GenerateAppProgramCs(string projectName, AppRequest app, SolutionGenerationRequest request)
        {
            return app.Type switch
            {
                AppType.BlazorWebAssembly => GenerateBlazorWebAssemblyProgramCs(projectName, app, request),
                AppType.BlazorServer => GenerateBlazorServerProgramCs(projectName, app, request),
                AppType.Mvc => GenerateMvcProgramCs(projectName, app, request),
                AppType.Maui => GenerateMauiProgramCs(projectName, app, request),
                _ => GenerateBlazorWebAssemblyProgramCs(projectName, app, request)
            };
        }

        private string GenerateBlazorWebAssemblyProgramCs(string projectName, AppRequest app, SolutionGenerationRequest request)
        {
            return $@"using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using {projectName};

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>(""#app"");
builder.RootComponents.Add<HeadOutlet>(""head::after"");

builder.Services.AddScoped(sp => new HttpClient {{ BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }});

await builder.Build().RunAsync();";
        }

        private string GenerateBlazorServerProgramCs(string projectName, AppRequest app, SolutionGenerationRequest request)
        {
            return $@"using {projectName}.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{{
    app.UseExceptionHandler(""/Error"");
    app.UseHsts();
}}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage(""/_Host"");

app.Run();";
        }

        private string GenerateMvcProgramCs(string projectName, AppRequest app, SolutionGenerationRequest request)
        {
            return $@"var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{{
    app.UseExceptionHandler(""/Home/Error"");
    app.UseHsts();
}}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: ""default"",
    pattern: ""{{controller=Home}}/{{action=Index}}/{{id?}}"");

app.Run();";
        }

        private string GenerateMauiProgramCs(string projectName, AppRequest app, SolutionGenerationRequest request)
        {
            return $@"using Microsoft.Extensions.Logging;

namespace {projectName};

public static class MauiProgram
{{
    public static MauiApp CreateMauiApp()
    {{
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {{
                fonts.AddFont(""OpenSans-Regular.ttf"", ""OpenSansRegular"");
            }});

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }}
}}";
        }

        #endregion





























        //private async Task GenerateSharedFilesAsync(string solutionPath, SolutionGenerationRequest request)
        //{
        //    // README.md
        //    var readmeContent = GenerateReadmeContent(request);
        //    await File.WriteAllTextAsync(Path.Combine(solutionPath, "README.md"), readmeContent);

        //    // docker-compose.yml
        //    var dockerComposeContent = GenerateDockerComposeContent(request);
        //    await File.WriteAllTextAsync(Path.Combine(solutionPath, "docker", "docker-compose.yml"), dockerComposeContent);

        //    // docker-compose.override.yml
        //    var dockerOverrideContent = GenerateDockerOverrideContent(request);
        //    await File.WriteAllTextAsync(Path.Combine(solutionPath, "docker", "docker-compose.override.yml"), dockerOverrideContent);

        //    // .gitignore
        //    var gitignoreContent = GenerateGitignoreContent();
        //    await File.WriteAllTextAsync(Path.Combine(solutionPath, ".gitignore"), gitignoreContent);

        //    // Generate gateways
        //    await GenerateGatewaysAsync(solutionPath, request);

        //    // Generate apps
        //    await GenerateAppsAsync(solutionPath, request);

        //    // Generate auth server
        //    await GenerateAuthServerAsync(solutionPath, request);

        //    // Generate shared projects
        //    await GenerateSharedProjectsAsync(solutionPath, request);

        //    // Generate build scripts
        //    await GenerateBuildScriptsAsync(solutionPath, request);

        //    // Generate Kubernetes manifests if enabled
        //    if (request.SharedInfrastructure.EnableKubernetes)
        //    {
        //        await GenerateKubernetesManifestsAsync(solutionPath, request);
        //    }
        //}


        //private async Task GenerateSolutionFileAsync(string solutionPath, SolutionGenerationRequest request)
        //{
        //    Logger.LogInformation($"Creating solution file for: {request.SolutionName}");

        //    var solutionContent = GenerateSolutionFileContent(request);
        //    var solutionFilePath = Path.Combine(solutionPath, $"{request.SolutionName}.sln");
        //    await File.WriteAllTextAsync(solutionFilePath, solutionContent);

        //    Logger.LogInformation($"Created solution file: {solutionFilePath}");
        //}

        //private string GenerateSolutionFileContent(SolutionGenerationRequest request)
        //{
        //    var sb = new StringBuilder();
        //    sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
        //    sb.AppendLine("# Visual Studio Version 17");
        //    sb.AppendLine("VisualStudioVersion = 17.0.31903.59");
        //    sb.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");
        //    sb.AppendLine();

        //    // إضافة مشاريع الخدمات المصغرة
        //    foreach (var microservice in request.Microservices)
        //    {
        //        sb.AppendLine($"# {microservice.Name} Microservice");
        //        var projects = new[]
        //        {
        //            $"{request.CompanyName}.{microservice.Name}.Domain.Shared",
        //            $"{request.CompanyName}.{microservice.Name}.Domain",
        //            $"{request.CompanyName}.{microservice.Name}.Application.Contracts",
        //            $"{request.CompanyName}.{microservice.Name}.Application",
        //            $"{request.CompanyName}.{microservice.Name}.EntityFrameworkCore",
        //            $"{request.CompanyName}.{microservice.Name}.HttpApi",
        //            $"{request.CompanyName}.{microservice.Name}.HttpApi.Client"
        //        };

        //        foreach (var project in projects)
        //        {
        //            var projectGuid = Guid.NewGuid().ToString().ToUpper();
        //            var projectPath = $"services\\{microservice.Name}\\src\\{project}\\{project}.csproj";
        //            sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{project}\", \"{projectPath}\", \"{{{projectGuid}}}\"");
        //            sb.AppendLine("EndProject");
        //        }

        //        // إضافة Host project
        //        var hostProject = $"{request.CompanyName}.{microservice.Name}.HttpApi.Host";
        //        var hostGuid = Guid.NewGuid().ToString().ToUpper();
        //        var hostPath = $"services\\{microservice.Name}\\host\\{hostProject}\\{hostProject}.csproj";
        //        sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{hostProject}\", \"{hostPath}\", \"{{{hostGuid}}}\"");
        //        sb.AppendLine("EndProject");
        //        sb.AppendLine();
        //    }

        //    // إضافة مشاريع Gateways
        //    foreach (var gateway in request.Gateways)
        //    {
        //        var gatewayProject = $"{request.CompanyName}.{gateway.Name}";
        //        var gatewayGuid = Guid.NewGuid().ToString().ToUpper();
        //        var gatewayPath = $"gateways\\{gateway.Name}\\{gatewayProject}.csproj";
        //        sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{gatewayProject}\", \"{gatewayPath}\", \"{{{gatewayGuid}}}\"");
        //        sb.AppendLine("EndProject");
        //    }

        //    // إضافة مشاريع Apps
        //    foreach (var app in request.Apps)
        //    {
        //        var appProject = $"{request.CompanyName}.{app.Name}";
        //        var appGuid = Guid.NewGuid().ToString().ToUpper();
        //        var appPath = $"apps\\{app.Name}\\{appProject}.csproj";
        //        sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{appProject}\", \"{appPath}\", \"{{{appGuid}}}\"");
        //        sb.AppendLine("EndProject");
        //    }

        //    // إضافة Auth Server
        //    var authProject = $"{request.CompanyName}.{request.AuthService.Name}";
        //    var authGuid = Guid.NewGuid().ToString().ToUpper();
        //    var authPath = $"services\\{request.AuthService.Name}\\{authProject}.csproj";
        //    sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{authProject}\", \"{authPath}\", \"{{{authGuid}}}\"");
        //    sb.AppendLine("EndProject");

        //    // إضافة Shared projects
        //    var sharedHosting = $"{request.CompanyName}.Shared.Hosting";
        //    var sharedLocalization = $"{request.CompanyName}.Shared.Localization";
        //    var sharedHostingGuid = Guid.NewGuid().ToString().ToUpper();
        //    var sharedLocalizationGuid = Guid.NewGuid().ToString().ToUpper();

        //    sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{sharedHosting}\", \"shared\\{sharedHosting}\\{sharedHosting}.csproj\", \"{{{sharedHostingGuid}}}\"");
        //    sb.AppendLine("EndProject");
        //    sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{sharedLocalization}\", \"shared\\{sharedLocalization}\\{sharedLocalization}.csproj\", \"{{{sharedLocalizationGuid}}}\"");
        //    sb.AppendLine("EndProject");

        //    return sb.ToString();
        //}

        //private async Task GenerateSharedProjectsAsync(string solutionPath, SolutionGenerationRequest request)
        //{
        //    var sharedPath = Path.Combine(solutionPath, "shared");

        //    // Create shared hosting project
        //    var hostingProject = $"{request.CompanyName}.Shared.Hosting";
        //    Directory.CreateDirectory(Path.Combine(sharedPath, hostingProject));

        //    var hostingContent = GenerateSharedHostingContent(hostingProject, request);
        //    await File.WriteAllTextAsync(Path.Combine(sharedPath, hostingProject, $"{hostingProject}.csproj"), hostingContent);

        //    // Create shared localization project
        //    var localizationProject = $"{request.CompanyName}.Shared.Localization";
        //    Directory.CreateDirectory(Path.Combine(sharedPath, localizationProject));

        //    var localizationContent = GenerateSharedLocalizationContent(localizationProject, request);
        //    await File.WriteAllTextAsync(Path.Combine(sharedPath, localizationProject, $"{localizationProject}.csproj"), localizationContent);
        //}



        //        private string GenerateSharedHostingContent(string projectName, SolutionGenerationRequest request)
        //        {
        //            return $@"<Project Sdk=""Microsoft.NET.Sdk"">
        //  <PropertyGroup>
        //    <TargetFramework>net9.0</TargetFramework>
        //  </PropertyGroup>
        //  <ItemGroup>
        //    <PackageReference Include=""Volo.Abp.AspNetCore.Serilog"" Version=""4.2.0"" />
        //  </ItemGroup>
        //</Project>";
        //        }

        //        private string GenerateSharedLocalizationContent(string projectName, SolutionGenerationRequest request)
        //        {
        //            return $@"<Project Sdk=""Microsoft.NET.Sdk"">
        //  <PropertyGroup>
        //    <TargetFramework>net9.0</TargetFramework>
        //  </PropertyGroup>
        //  <ItemGroup>
        //    <PackageReference Include=""Volo.Abp.Localization"" Version=""4.2.0"" />
        //  </ItemGroup>
        //</Project>";
        //        }





















        #region not for now
        private async Task GenerateBuildScriptsAsync(string solutionPath, SolutionGenerationRequest request)
        {
            var buildPath = Path.Combine(solutionPath, "build");
            Directory.CreateDirectory(buildPath); // Ensure build directory exists
            // Create build scripts
            var buildScript = GenerateBuildScript(request);
            await File.WriteAllTextAsync(Path.Combine(buildPath, "build.ps1"), buildScript);

            var buildShScript = GenerateBuildShScript(request);
            await File.WriteAllTextAsync(Path.Combine(buildPath, "build.sh"), buildShScript);
        }

        private async Task GenerateKubernetesManifestsAsync(string solutionPath, SolutionGenerationRequest request)
        {
            var k8sPath = Path.Combine(solutionPath, "k8s");

            // Generate namespace
            var namespaceContent = GenerateK8sNamespace(request);
            await File.WriteAllTextAsync(Path.Combine(k8sPath, "namespace.yaml"), namespaceContent);

            // Generate services manifests
            foreach (var service in request.Microservices)
            {
                var serviceManifest = GenerateK8sServiceManifest(service, request);
                await File.WriteAllTextAsync(Path.Combine(k8sPath, $"{service.Name.ToLower()}-service.yaml"), serviceManifest);
            }
        }

        #endregion
    }
}
