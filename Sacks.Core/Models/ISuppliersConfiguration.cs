namespace Sacks.Core.FileProcessing.Configuration
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ISuppliersConfiguration
    {
        public Dictionary<string, Dictionary<string, string>> Lookups { get;}
        List<SupplierConfiguration> Suppliers { get;}

        Task Save();
        IList<string> ValidateConfiguration();
    }
}
