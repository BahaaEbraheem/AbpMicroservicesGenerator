using System.ComponentModel.DataAnnotations;

namespace AbpMicroservicesGenerator.SolutionGeneration
{
    public class GatewayRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        [Display(Name = "Gateway Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1000, 65535)]
        [Display(Name = "Port")]
        public int Port { get; set; } = 44300;

        [Display(Name = "Gateway Type")]
        public GatewayType Type { get; set; } = GatewayType.WebGateway;

        [Display(Name = "Enable Rate Limiting")]
        public bool EnableRateLimiting { get; set; } = true;

        [Display(Name = "Enable Load Balancing")]
        public bool EnableLoadBalancing { get; set; } = true;

        [Display(Name = "Enable Authentication")]
        public bool EnableAuthentication { get; set; } = true;

        [Display(Name = "Enable CORS")]
        public bool EnableCors { get; set; } = true;

        [Display(Name = "Enable Swagger")]
        public bool EnableSwagger { get; set; } = true;
    }

    public enum GatewayType
    {
        [Display(Name = "Web Gateway")]
        WebGateway,
        
        [Display(Name = "Public Web Gateway")]
        PublicWebGateway,
        
        [Display(Name = "Internal Gateway")]
        InternalGateway
    }
}
