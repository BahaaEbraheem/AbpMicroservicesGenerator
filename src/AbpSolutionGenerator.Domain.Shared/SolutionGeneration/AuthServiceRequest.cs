using System.ComponentModel.DataAnnotations;

namespace AbpSolutionGenerator.SolutionGeneration
{
    public class AuthServiceRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        [Display(Name = "Service Name")]
        public string Name { get; set; } = "AuthServer";

        [StringLength(200)]
        [Display(Name = "Description")]
        public string Description { get; set; } = "Authentication and Authorization Service";

        [Required]
        [Range(1000, 65535)]
        [Display(Name = "Port")]
        public int Port { get; set; } = 44322;

        [Display(Name = "Identity Provider")]
        public IdentityProvider Provider { get; set; } = IdentityProvider.OpenIddict;

        [Display(Name = "Enable External Logins")]
        public bool EnableExternalLogins { get; set; } = true;

        [Display(Name = "Enable Google Login")]
        public bool EnableGoogleLogin { get; set; } = false;

        [Display(Name = "Enable Microsoft Login")]
        public bool EnableMicrosoftLogin { get; set; } = false;

        [Display(Name = "Enable Facebook Login")]
        public bool EnableFacebookLogin { get; set; } = false;

        [Display(Name = "Enable Twitter Login")]
        public bool EnableTwitterLogin { get; set; } = false;

        [Display(Name = "Enable LDAP")]
        public bool EnableLdap { get; set; } = false;

        [Display(Name = "Enable Two Factor Authentication")]
        public bool EnableTwoFactor { get; set; } = true;

        [Display(Name = "Enable Account Lockout")]
        public bool EnableAccountLockout { get; set; } = true;

        [Display(Name = "Enable CAPTCHA")]
        public bool EnableCaptcha { get; set; } = false;

        [Display(Name = "Token Lifetime (minutes)")]
        [Range(1, 10080)] // 1 minute to 1 week
        public int TokenLifetimeMinutes { get; set; } = 60;

        [Display(Name = "Refresh Token Lifetime (days)")]
        [Range(1, 365)]
        public int RefreshTokenLifetimeDays { get; set; } = 30;
    }

    public enum IdentityProvider
    {
        [Display(Name = "OpenIddict")]
        OpenIddict,
        
        [Display(Name = "IdentityServer4")]
        IdentityServer4,
        
        [Display(Name = "Duende IdentityServer")]
        DuendeIdentityServer
    }
}
