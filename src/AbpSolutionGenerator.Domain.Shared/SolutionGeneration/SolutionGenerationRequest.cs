using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AbpSolutionGenerator.SolutionGeneration
{
    public class SolutionGenerationRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        [Display(Name = "Solution Name")]
        public string SolutionName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Company Name")]
        [StringLength(100, MinimumLength = 2)]
        public string CompanyName { get; set; } = string.Empty;

        [Display(Name = "Enable Multi-Tenancy")]
        public bool EnableMultiTenancy { get; set; } = true;

        [Display(Name = "Enable SaaS")]
        public bool EnableSaas { get; set; } = true;

        [Display(Name = "Database Provider")]
        public DatabaseProvider DatabaseProvider { get; set; } = DatabaseProvider.SqlServer;

        [Display(Name = "UI Framework")]
        public UIFramework UIFramework { get; set; } = UIFramework.BlazorWebApp;

        [Required]
        [MinLength(1, ErrorMessage = "At least one microservice is required")]
        public List<MicroserviceRequest> Microservices { get; set; } = new();

        [Display(Name = "API Gateways")]
        public List<GatewayRequest> Gateways { get; set; } = new();

        [Display(Name = "Client Applications")]
        public List<AppRequest> Apps { get; set; } = new();

        [Display(Name = "Authentication Service")]
        public AuthServiceRequest AuthService { get; set; } = new();

        [Display(Name = "Shared Infrastructure")]
        public SharedInfrastructureRequest SharedInfrastructure { get; set; } = new();

        public SolutionGenerationRequest()
        {
            // Add default microservices
            Microservices.Add(new MicroserviceRequest
            {
                Name = "IdentityService",
                Description = "Identity and user management service",
                Port = 44305
            });

            Microservices.Add(new MicroserviceRequest
            {
                Name = "AdministrationService",
                Description = "Administration and settings service",
                Port = 44306
            });

            // Add default gateways
            Gateways.Add(new GatewayRequest
            {
                Name = "WebGateway",
                Description = "Main web gateway for client applications",
                Port = 44325,
                Type = GatewayType.WebGateway
            });

            Gateways.Add(new GatewayRequest
            {
                Name = "PublicWebGateway",
                Description = "Public web gateway for external access",
                Port = 44335,
                Type = GatewayType.PublicWebGateway
            });

            // Add default apps
            Apps.Add(new AppRequest
            {
                Name = "WebApp",
                Description = "Main web application",
                Port = 44303,
                Type = AppType.BlazorWebAssembly
            });

            Apps.Add(new AppRequest
            {
                Name = "PublicWebApp",
                Description = "Public web application",
                Port = 44304,
                Type = AppType.BlazorWebAssembly
            });

            // Initialize auth service with defaults
            AuthService = new AuthServiceRequest();

            // Initialize shared infrastructure with defaults
            SharedInfrastructure = new SharedInfrastructureRequest();
        }
    }
}
