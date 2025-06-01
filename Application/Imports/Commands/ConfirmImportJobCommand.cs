using Application.Abstractions.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Imports.Commands
{
    public sealed record ConfirmImportJobCommand(Guid ImportJobId) : ICommand<bool>;
}
