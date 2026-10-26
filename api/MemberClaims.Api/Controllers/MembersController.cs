// Member Claims Demo
// Copyright (c) 2026 Ahmed Ibrahim (https://www.linkedin.com/in/ahmed-ibrahim-151a21182)
// Licensed under the MIT License. See LICENSE in the repository root.

using MemberClaims.Api.Dtos;
using MemberClaims.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MemberClaims.Api.Controllers;

/// <summary>
/// Thin controller: HTTP concerns only. Business logic lives in IMemberService,
/// which is injected through the constructor (dependency injection).
/// </summary>
[ApiController]
[Route("api/members")]
[Produces("application/json")]
public sealed class MembersController(IMemberService members) : ControllerBase
{
    /// <summary>Search members by last name, first name or member number (prefix match), with paging.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<MemberSummaryDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await members.SearchAsync(search, page, pageSize, cancellationToken));
    }

    /// <summary>Get one member with their claims.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDetailDto>> Get(int id, CancellationToken cancellationToken)
    {
        return Ok(await members.GetAsync(id, cancellationToken));
    }

    /// <summary>Submit a new claim for a member.</summary>
    [HttpPost("{id:int}/claims")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClaimDto>> AddClaim(
        int id,
        CreateClaimRequest request,
        CancellationToken cancellationToken)
    {
        // [ApiController] has already validated the request (400 with details if invalid).
        var claim = await members.AddClaimAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id }, claim);
    }
}
