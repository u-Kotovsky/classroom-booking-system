using ClassroomBooking.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClassroomBooking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(
        CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var booking = await _bookingService.CreateBookingAsync(request, cancellationToken);

            return Created($"/api/bookings/{booking.Id}", booking);
        }
        catch (ContractOperationException ex)
        {
            return StatusCode(MapStatus(ex.ErrorCode), new { error = ex.ErrorCode });
        }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<BookingDto>> Approve(
        Guid id,
        ApproveBookingRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var approveRequest = request with { BookingId = id };

            var booking = await _bookingService.ApproveBookingAsync(approveRequest, cancellationToken);

            return Ok(booking);
        }
        catch (ContractOperationException ex)
        {
            return StatusCode(MapStatus(ex.ErrorCode), new { error = ex.ErrorCode });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<BookingDto>> Cancel(
        Guid id,
        [FromQuery] Guid userId,
        [FromQuery] bool isAdmin,
        CancellationToken cancellationToken)
    {
        try
        {
            var booking = await _bookingService.CancelBookingAsync(
                id,
                userId,
                isAdmin,
                cancellationToken);

            return Ok(booking);
        }
        catch (ContractOperationException ex)
        {
            return StatusCode(MapStatus(ex.ErrorCode), new { error = ex.ErrorCode });
        }
    }

    private static int MapStatus(string errorCode)
    {
        return errorCode switch
        {
            "SCHEDULE_CONFLICT" => StatusCodes.Status409Conflict,
            "SLOT_ALREADY_RESERVED" => StatusCodes.Status409Conflict,
            "SLOT_NOT_RESERVED" => StatusCodes.Status409Conflict,
            "BOOKING_NOT_FOUND" => StatusCodes.Status404NotFound,
            "CLASSROOM_NOT_FOUND" => StatusCodes.Status404NotFound,
            "INVALID_TIME_INTERVAL" => StatusCodes.Status400BadRequest,
            "INVALID_BOOKING_STATUS" => StatusCodes.Status400BadRequest,
            "FORBIDDEN" => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };
    }
}