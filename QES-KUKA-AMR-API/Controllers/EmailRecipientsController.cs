using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QES_KUKA_AMR_API.Data.Entities;
using QES_KUKA_AMR_API.Models;
using QES_KUKA_AMR_API.Models.EmailRecipients;
using QES_KUKA_AMR_API.Services.Email;
using QES_KUKA_AMR_API.Services.EmailRecipients;

namespace QES_KUKA_AMR_API.Controllers;

/// <summary>
/// Controller for managing email notification recipients.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/email-recipients")]
public class EmailRecipientsController : ControllerBase
{
    private readonly IEmailRecipientService _recipientService;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailRecipientsController> _logger;

    public EmailRecipientsController(
        IEmailRecipientService recipientService,
        IEmailService emailService,
        ILogger<EmailRecipientsController> logger)
    {
        _recipientService = recipientService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all email recipients.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EmailRecipientDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<EmailRecipientDto>>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var recipients = await _recipientService.GetAllAsync(cancellationToken);
        var dtos = recipients.Select(MapToDto).ToList();

        return Ok(new ApiResponse<List<EmailRecipientDto>>
        {
            Success = true,
            Data = dtos
        });
    }

    /// <summary>
    /// Gets an email recipient by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<EmailRecipientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EmailRecipientDto>>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var recipient = await _recipientService.GetByIdAsync(id, cancellationToken);

        if (recipient is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Email recipient not found",
                Detail = $"Email recipient with ID '{id}' was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(new ApiResponse<EmailRecipientDto>
        {
            Success = true,
            Data = MapToDto(recipient)
        });
    }

    /// <summary>
    /// Creates a new email recipient.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EmailRecipientDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<EmailRecipientDto>>> CreateAsync(
        [FromBody] EmailRecipientCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var username = GetCurrentUsername();

            var entity = await _recipientService.CreateAsync(new EmailRecipient
            {
                EmailAddress = request.EmailAddress,
                DisplayName = request.DisplayName,
                Description = request.Description,
                NotificationTypes = request.NotificationTypes,
                IsActive = request.IsActive
            }, username, cancellationToken);

            var dto = MapToDto(entity);

            return CreatedAtAction(
                nameof(GetByIdAsync),
                new { id = dto.Id },
                new ApiResponse<EmailRecipientDto>
                {
                    Success = true,
                    Data = dto,
                    Msg = "Email recipient created successfully"
                });
        }
        catch (EmailRecipientConflictException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Email recipient already exists",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    /// <summary>
    /// Updates an existing email recipient.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<EmailRecipientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<EmailRecipientDto>>> UpdateAsync(
        int id,
        [FromBody] EmailRecipientUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var username = GetCurrentUsername();

            var updated = await _recipientService.UpdateAsync(id, new EmailRecipient
            {
                EmailAddress = request.EmailAddress,
                DisplayName = request.DisplayName,
                Description = request.Description,
                NotificationTypes = request.NotificationTypes,
                IsActive = request.IsActive
            }, username, cancellationToken);

            if (updated is null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Email recipient not found",
                    Detail = $"Email recipient with ID '{id}' was not found.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Ok(new ApiResponse<EmailRecipientDto>
            {
                Success = true,
                Data = MapToDto(updated),
                Msg = "Email recipient updated successfully"
            });
        }
        catch (EmailRecipientConflictException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Email recipient already exists",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    /// <summary>
    /// Deletes an email recipient.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _recipientService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Email recipient not found",
                Detail = $"Email recipient with ID '{id}' was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Msg = "Email recipient deleted successfully"
        });
    }

    /// <summary>
    /// Sends a test email to verify SMTP configuration.
    /// </summary>
    [HttpPost("test-email")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> SendTestEmailAsync(
        [FromBody] TestEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!_emailService.IsEnabled)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Email service disabled",
                Detail = "The email service is currently disabled. Check SMTP configuration in appsettings.json.",
                Status = StatusCodes.Status500InternalServerError
            });
        }

        var subject = "[Test] KUKA AMR Email Notification System";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #ff6600; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 15px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>KUKA AMR Email Test</h2>
        </div>
        <div class='content'>
            <p>This is a test email to verify the SMTP configuration is working correctly.</p>
            <p><strong>Sent at:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
            <p><strong>Server:</strong> {Environment.MachineName}</p>
            <p><strong>Sent by:</strong> {GetCurrentUsername() ?? "Unknown"}</p>
        </div>
        <div class='footer'>
            This is an automated test message from KUKA AMR System.
        </div>
    </div>
</body>
</html>";

        var success = await _emailService.SendEmailAsync(
            request.TestEmailAddress,
            "Test Recipient",
            subject,
            body,
            cancellationToken);

        if (success)
        {
            _logger.LogInformation(
                "Test email sent to {Email} by {User}",
                request.TestEmailAddress, GetCurrentUsername());

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Msg = $"Test email sent successfully to {request.TestEmailAddress}"
            });
        }

        return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
        {
            Title = "Failed to send test email",
            Detail = "Check SMTP configuration and server logs for details.",
            Status = StatusCodes.Status500InternalServerError
        });
    }

    private string? GetCurrentUsername()
    {
        return User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("username")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static EmailRecipientDto MapToDto(EmailRecipient entity) => new()
    {
        Id = entity.Id,
        EmailAddress = entity.EmailAddress,
        DisplayName = entity.DisplayName,
        Description = entity.Description,
        NotificationTypes = entity.NotificationTypes,
        IsActive = entity.IsActive,
        CreatedUtc = entity.CreatedUtc,
        UpdatedUtc = entity.UpdatedUtc,
        CreatedBy = entity.CreatedBy,
        UpdatedBy = entity.UpdatedBy
    };
}
