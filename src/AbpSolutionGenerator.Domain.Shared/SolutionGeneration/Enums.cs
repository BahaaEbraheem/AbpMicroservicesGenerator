using System.ComponentModel.DataAnnotations;

namespace AbpSolutionGenerator.SolutionGeneration
{
    public enum DatabaseProvider
    {
        [Display(Name = "SQL Server")]
        SqlServer = 0,
        
        [Display(Name = "MySQL")]
        MySQL = 1,
        
        [Display(Name = "PostgreSQL")]
        PostgreSQL = 2,
        
        [Display(Name = "SQLite")]
        SQLite = 3,
        
        [Display(Name = "Oracle")]
        Oracle = 4
    }

    public enum UIFramework
    {
        [Display(Name = "Blazor WebApp")]
        BlazorWebApp = 0,
        
        [Display(Name = "Blazor WebAssembly")]
        BlazorWebAssembly = 1,
        
        [Display(Name = "MVC")]
        MVC = 2,
        
        [Display(Name = "Angular")]
        Angular = 3,
        
        [Display(Name = "React")]
        React = 4,
        
        [Display(Name = "Vue")]
        Vue = 5
    }

    public enum ProjectType
    {
        [Display(Name = "Microservice Solution")]
        MicroserviceSolution = 0,
        
        [Display(Name = "Modular Application")]
        ModularApplication = 1,
        
        [Display(Name = "Single Application")]
        SingleApplication = 2
    }

    public enum GenerationStatus
    {
        [Display(Name = "Pending")]
        Pending = 0,
        
        [Display(Name = "In Progress")]
        InProgress = 1,
        
        [Display(Name = "Completed")]
        Completed = 2,
        
        [Display(Name = "Failed")]
        Failed = 3,
        
        [Display(Name = "Cancelled")]
        Cancelled = 4
    }
}
