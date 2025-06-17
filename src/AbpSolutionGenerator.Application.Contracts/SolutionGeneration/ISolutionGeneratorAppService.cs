using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace AbpSolutionGenerator.SolutionGeneration
{
    public interface ISolutionGeneratorAppService : IApplicationService
    {
        Task<SolutionGenerationResponse> GenerateSolutionAsync(SolutionGenerationRequest request);
        Task<SolutionGenerationResponse> GetGenerationStatusAsync(Guid id);
        Task<byte[]> DownloadSolutionAsync(Guid id);
        Task<bool> ValidateSolutionNameAsync(string solutionName);
        Task<bool> ValidateMicroserviceNameAsync(string microserviceName);
        Task<int> GetNextAvailablePortAsync();
        Task<int> GetNextAvailablePortWithExclusionsAsync(List<int> usedPorts);
    }
}
