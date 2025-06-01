using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Imports
{
    public static class ImportJobErrors
    {
        public static Error NotFound(Guid importJobId) => Error.NotFound(
            "ImportJobs.NotFound",
            $"The import job with Id = '{importJobId}' was not found");

        public static Error Unauthorized() => Error.Failure(
            "ImportJobs.Unauthorized",
            "You are not authorized to access this import job");

        public static Error CannotCancel(Guid importJobId) => Error.Problem(
            "ImportJobs.CannotCancel",
            $"The import job with Id = '{importJobId}' cannot be cancelled in its current state");

        public static Error AlreadyCompleted(Guid importJobId) => Error.Problem(
            "ImportJobs.AlreadyCompleted",
            $"The import job with Id = '{importJobId}' has already been completed");

        public static Error InvalidFile() => Error.Problem(
            "ImportJobs.InvalidFile",
            "The uploaded file is invalid or corrupted");

        public static Error FileTooLarge(long maxSizeBytes) => Error.Problem(
            "ImportJobs.FileTooLarge",
            $"The uploaded file exceeds the maximum allowed size of {maxSizeBytes / (1024 * 1024)}MB");

        public static Error UnsupportedFileType(string supportedTypes) => Error.Problem(
            "ImportJobs.UnsupportedFileType",
            $"Only the following file types are supported: {supportedTypes}");
    }
}
