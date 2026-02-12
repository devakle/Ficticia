using BuildingBlocks.Abstractions.Common;
using MediatR;

namespace Modules.People.Application.People.Commands;

public sealed record SetPersonStatusCommand(Guid Id, bool IsActive) : IRequest<Result>;
