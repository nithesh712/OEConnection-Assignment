using MediatR;
using Microsoft.EntityFrameworkCore;
using RL.Backend.Exceptions;
using RL.Backend.Models;
using RL.Data;
using RL.Data.DataModels;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RL.Backend.Commands.Handlers.Plans
{
    public class AddUserToProcedureCommandHandler : IRequestHandler<AddUserToProcedureCommand, ApiResponse<Unit>>
    {
        private readonly RLContext _context;

        public AddUserToProcedureCommandHandler(RLContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<Unit>> Handle(AddUserToProcedureCommand request, CancellationToken cancellationToken)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (request.UserId.Count > 0)
                    {
                        var assignedUsers = await _context.AssignedUsers
                            .Where(au => au.PlanId == request.PlanId && au.ProcedureId == request.ProcedureId)
                            .ToListAsync();

                        if (assignedUsers != null || assignedUsers.Count != 0)
                        {
                            _context.AssignedUsers.RemoveRange(assignedUsers);
                        }
                    }
                    if (request.PlanId < 1)
                        return ApiResponse<Unit>.Fail(new BadRequestException("Invalid PlanId"));
                    if (request.ProcedureId < 1)
                        return ApiResponse<Unit>.Fail(new BadRequestException("Invalid ProcedureId"));
                    if (request.UserId.Count < 1)
                        return ApiResponse<Unit>.Fail(new BadRequestException("Invalid UserId"));

                    var plan = await _context.Plans
                        .Include(p => p.PlanProcedures)
                        .ThenInclude(pp => pp.AssignedUsers)
                        .FirstOrDefaultAsync(p => p.PlanId == request.PlanId);

                    var procedure = await _context.Procedures
                        .FirstOrDefaultAsync(p => p.ProcedureId == request.ProcedureId);

                    if (plan is null || procedure is null)
                    {
                        return ApiResponse<Unit>.Fail(new NotFoundException("Plan or Procedure not found"));
                    }

                    // Check if the procedure is associated with the plan
                    if (!plan.PlanProcedures.Any(pp => pp.ProcedureId == procedure.ProcedureId))
                    {
                        return ApiResponse<Unit>.Fail(new BadRequestException($"ProcedureId: {request.ProcedureId} is not associated with PlanId: {request.PlanId}"));
                    }

                    var newAssignedUsers = new List<AssignedUser>();

                    foreach (var userId in request.UserId)
                    {
                        // Check if the user is already assigned to the procedure within the plan
                        var isUserAlreadyAssigned = plan.PlanProcedures
                            .Any(pp => pp.ProcedureId == procedure.ProcedureId && pp.AssignedUsers.Any(au => au.UserId == userId));

                        if (!isUserAlreadyAssigned)
                        {
                            newAssignedUsers.Add(new AssignedUser
                            {
                                ProcedureId = procedure.ProcedureId,
                                UserId = userId,
                                PlanId = plan.PlanId
                            });
                        }
                    }

                    _context.AssignedUsers.AddRange(newAssignedUsers);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return ApiResponse<Unit>.Succeed(new Unit());
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    return ApiResponse<Unit>.Fail(e);
                }
            }
        }
    }
}
