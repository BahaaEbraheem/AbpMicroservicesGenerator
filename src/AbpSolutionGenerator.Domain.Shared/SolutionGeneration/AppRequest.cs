using System.ComponentModel.DataAnnotations;

namespace AbpSolutionGenerator.SolutionGeneration
{
    public class AppRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        [Display(Name = "Application Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1000, 65535)]
        [Display(Name = "Port")]
        public int Port { get; set; } = 44301;

        [Display(Name = "Application Type")]
        public AppType Type { get; set; } = AppType.BlazorWebAssembly;

        [Display(Name = "Enable PWA")]
        public bool EnablePwa { get; set; } = false;

        [Display(Name = "Enable Authentication")]
        public bool EnableAuthentication { get; set; } = true;

        [Display(Name = "Enable Multi-Tenancy")]
        public bool EnableMultiTenancy { get; set; } = true;

        [Display(Name = "Enable Localization")]
        public bool EnableLocalization { get; set; } = true;

        [Display(Name = "Theme")]
        public AppTheme Theme { get; set; } = AppTheme.LeptonXLite;
    }

    public enum AppType
    {
        [Display(Name = "Blazor WebAssembly")]
        BlazorWebAssembly,
        
        [Display(Name = "Blazor Server")]
        BlazorServer,
        
        [Display(Name = "MVC")]
        Mvc,
        
        [Display(Name = "Angular")]
        Angular,
        
        [Display(Name = "React")]
        React,
        
        [Display(Name = "Mobile (MAUI)")]
        Maui
    }

    public enum AppTheme
    {
        [Display(Name = "LeptonX Lite")]
        LeptonXLite,
        
        [Display(Name = "LeptonX")]
        LeptonX,
        
        [Display(Name = "Basic")]
        Basic,
        
        [Display(Name = "Lepton")]
        Lepton
    }
}
