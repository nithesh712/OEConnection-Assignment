using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using RL.Backend.Commands;
using RL.Backend.Commands.Handlers.Plans;
using RL.Backend.Models;
using RL.Data;
using RL.Data.DataModels;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RL.Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class PlanController : ControllerBase
{
    private readonly ILogger<PlanController> _logger;
    private readonly RLContext _context;
    private readonly IMediator _mediator;

    public PlanController(ILogger<PlanController> logger, RLContext context, IMediator mediator)
    {
        _logger = logger;
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    [HttpGet]
    [EnableQuery]
    public IEnumerable<Plan> Get()
    {
        return _context.Plans;
    }

    [HttpPost]
    public async Task<IActionResult> PostPlan(CreatePlanCommand command, CancellationToken token)
    {
        var response = await _mediator.Send(command, token);

        return response.ToActionResult();
    }

    [HttpPost("AddProcedureToPlan")]
    public async Task<IActionResult> AddProcedureToPlan(AddProcedureToPlanCommand command, CancellationToken token)
    {
        var response = await _mediator.Send(command, token);

        return response.ToActionResult();
    }

    [HttpPost("AssignUserToProcedure")]
    public async Task<IActionResult> AssignUserToProcedure(AddUserToProcedureCommand command, CancellationToken token)
    {
        var response = await _mediator.Send(command, token);

        return response.ToActionResult();
    }

    [HttpGet("GetAssignedUsers")]
    public async Task<ActionResult<List<User>>> GetAssignedUsersWithProcedures(int planId, int procedureId)
    {
        try
        {
            var users = await (
                from assignedUser in _context.AssignedUsers
                where assignedUser.PlanId == planId && assignedUser.ProcedureId == procedureId
                join user in _context.Users on assignedUser.UserId equals user.UserId
                select new {
                    user.UserId,
                    user.Name,
                    user.CreateDate,
                    user.UpdateDate
                }
            ).ToListAsync();

            var jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve,
                WriteIndented = true,
                MaxDepth = 32
            };

            var serializedUsers = JsonSerializer.Serialize(users, jsonOptions);

            return Ok(serializedUsers);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error: {ex.Message}");
        }
    }

}
