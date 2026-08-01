using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaultLedger.Application.DTOs.Users;
using VaultLedger.Application.Interfaces;

namespace VaultLedger.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminUsersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<UserLookupDto>>> Search(
        [FromQuery] string q,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Ok(Array.Empty<UserLookupDto>());

        var users = await _unitOfWork.Users.SearchAsync(q, take, cancellationToken);
        return Ok(users.Select(u => new UserLookupDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role.ToString()
        }).ToList());
    }
}
