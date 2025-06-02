using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IImportBackgroundService
    {
        Task ValidateImportAsync(Guid jobId, bool skipDuplicates);
        Task ProcessImportAsync(Guid jobId);
        Task CancelImportAsync(Guid jobId);
    }
}
