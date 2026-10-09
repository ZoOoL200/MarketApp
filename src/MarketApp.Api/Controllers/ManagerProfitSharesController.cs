using MarketApp.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace MarketApp.Api.Controllers;
[ApiController, Route("api/branches/{branchId:guid}/manager-profit-shares"), Authorize(Roles = AppRoles.Stakeholder)]
public sealed class ManagerProfitSharesController : ControllerBase
{
    [HttpGet, HttpPost, ProducesResponseType(typeof(ProblemDetails), 410)]
    public IActionResult Retired() => Problem(statusCode: 410,
        detail: "Percentage sharing has been retired. Use the branch profit report for selling price minus baseline, and expenses for salaries.");
}
