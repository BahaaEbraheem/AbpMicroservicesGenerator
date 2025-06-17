using System.ComponentModel.DataAnnotations;

namespace AbpSolutionGenerator.SolutionGeneration
{
    public class SharedInfrastructureRequest
    {
        [Display(Name = "Enable Redis")]
        public bool EnableRedis { get; set; } = true;

        [Display(Name = "Redis Connection String")]
        public string RedisConnectionString { get; set; } = "localhost:6379";

        [Display(Name = "Enable RabbitMQ")]
        public bool EnableRabbitMQ { get; set; } = true;

        [Display(Name = "RabbitMQ Connection String")]
        public string RabbitMQConnectionString { get; set; } = "amqp://guest:guest@localhost:5672";

        [Display(Name = "Enable Elasticsearch")]
        public bool EnableElasticsearch { get; set; } = false;

        [Display(Name = "Elasticsearch URL")]
        public string ElasticsearchUrl { get; set; } = "http://localhost:9200";

        [Display(Name = "Enable Serilog")]
        public bool EnableSerilog { get; set; } = true;

        [Display(Name = "Enable Seq")]
        public bool EnableSeq { get; set; } = false;

        [Display(Name = "Seq URL")]
        public string SeqUrl { get; set; } = "http://localhost:5341";

        [Display(Name = "Enable Docker")]
        public bool EnableDocker { get; set; } = true;

        [Display(Name = "Enable Kubernetes")]
        public bool EnableKubernetes { get; set; } = false;

        [Display(Name = "Enable Helm Charts")]
        public bool EnableHelmCharts { get; set; } = false;

        [Display(Name = "Enable Monitoring")]
        public bool EnableMonitoring { get; set; } = true;

        [Display(Name = "Enable Prometheus")]
        public bool EnablePrometheus { get; set; } = false;

        [Display(Name = "Enable Grafana")]
        public bool EnableGrafana { get; set; } = false;

        [Display(Name = "Enable Health Checks")]
        public bool EnableHealthChecks { get; set; } = true;

        [Display(Name = "Enable Distributed Tracing")]
        public bool EnableDistributedTracing { get; set; } = false;

        [Display(Name = "Enable API Documentation")]
        public bool EnableApiDocumentation { get; set; } = true;

        [Display(Name = "Enable Shared Database")]
        public bool EnableSharedDatabase { get; set; } = false;

        [Display(Name = "Shared Database Name")]
        public string SharedDatabaseName { get; set; } = "SharedDb";
    }
}
