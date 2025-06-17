using System.ComponentModel.DataAnnotations;

namespace AbpSolutionGenerator.SolutionGeneration
{
    public class MicroserviceRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        [Display(Name = "Microservice Name")]
        [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9]*$", ErrorMessage = "Name must start with a letter and contain only letters and numbers")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1000, 65535, ErrorMessage = "Port must be between 1000 and 65535")]
        [Display(Name = "Port")]
        public int Port { get; set; } = 44300;

        [Display(Name = "Enable API")]
        public bool EnableApi { get; set; } = true;

        [Display(Name = "Enable gRPC")]
        public bool EnableGrpc { get; set; } = false;

        [Display(Name = "Enable Background Jobs")]
        public bool EnableBackgroundJobs { get; set; } = false;

        [Display(Name = "Enable Event Bus")]
        public bool EnableEventBus { get; set; } = true;

        [Display(Name = "Include Sample Entities")]
        public bool IncludeSampleEntities { get; set; } = false;

        public MicroserviceRequest()
        {
        }

        public MicroserviceRequest(string name, string description = "", int port = 44300)
        {
            Name = name;
            Description = description;
            Port = port;
        }
    }
}
