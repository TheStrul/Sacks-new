using System.ComponentModel.DataAnnotations;

namespace SacksDataLayer
{
    /// <summary>
    /// Tracks application deployments and versions
    /// </summary>
    public class ApplicationDeploymentEntity : Entity
    {
        /// <summary>
        /// Application version
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Version { get; set; } = "";

        /// <summary>
        /// Deployment timestamp
        /// </summary>
        public DateTime DeploymentDate { get; set; }

        /// <summary>
        /// Deployment environment (Development, Staging, Production)
        /// </summary>
        [MaxLength(50)]
        public string Environment { get; set; } = "Development";

        /// <summary>
        /// Deployment target (Local, Azure, AWS, etc.)
        /// </summary>
        [MaxLength(50)]
        public string Target { get; set; } = "Local";

        /// <summary>
        /// Deployment notes or change summary
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Who performed the deployment
        /// </summary>
        [MaxLength(100)]
        public string? DeployedBy { get; set; }

        /// <summary>
        /// Build/commit identifier
        /// </summary>
        [MaxLength(100)]
        public string? BuildId { get; set; }

        /// <summary>
        /// Indicates if this is the current active deployment
        /// </summary>
        public bool IsActive { get; set; }
    }
}
