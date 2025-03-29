using System;

namespace MaintainEase.DbMigrator.Plugins.MigrationPlugins.Models
{
    /// <summary>
    /// Class to hold DbContext information
    /// </summary>
    public class DbContextInfo
    {
        /// <summary>
        /// Gets or sets the simple name of the DbContext class
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the fully qualified name of the DbContext class
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the namespace of the DbContext class
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the assembly name containing the DbContext class
        /// </summary>
        public string Assembly { get; set; }

        /// <summary>
        /// Gets or sets the source file path (if discovered via source code analysis)
        /// </summary>
        public string SourceFile { get; set; }
    }
}
