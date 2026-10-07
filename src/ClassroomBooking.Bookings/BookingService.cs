using System.Collections.Concurrent;
using ClassroomBooking.Contracts;
using Microsoft.Extensions.Logging;

namespace ClassroomBooking.Bookings;

public sealed class BookingService : IBookingService
{
    private readonly ConcurrentDictionary<Guid, BookingDto> _bookings = new();
    private readonly IScheduleConflictService _conflictService;
    private readonly IScheduleReservationService _reservationService;
    private readonly IScheduleReleaseService _releaseService;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IScheduleConflictService conflictService,
        IScheduleReservationService reservationService,
        IScheduleReleaseService releaseService,
        ILogger<BookingService> logger)
    {
        _conflictService = conflictService;
        _reservationService = reservationService;
        _releaseService = releaseService;
        _logger = logger;
    }

    public async Task<BookingDto> CreateBookingAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EndTime <= request.StartTime)
        {
            _logger.LogWarning(
                "Booking creation rejected: {Operation} {Result} UserId={UserId} ClassroomId={ClassroomId} StartTime={StartTime} EndTime={EndTime} ErrorCode={ErrorCode}",
                "CreateBooking",
                "Rejected",
                request.UserId,
                request.ClassroomId,
                request.StartTime,
                request.EndTime,
                "INVALID_TIME_INTERVAL");

            throw new ContractOperationException("INVALID_TIME_INTERVAL");
        }

        var availability = await _conflictService.CheckAvailabilityAsync(
            new CheckAvailabilityRequest(request.ClassroomId, request.StartTime, request.EndTime),
            cancellationToken);

        if (!availability.IsAvailable)
        {
            var errorCode = availability.ErrorCode ?? "SCHEDULE_CONFLICT";

            _logger.LogWarning(
                "Booking creation rejected: {Operation} {Result} UserId={UserId} ClassroomId={ClassroomId} StartTime={StartTime} EndTime={EndTime} ErrorCode={ErrorCode}",
                "CreateBooking",
                "Rejected",
                request.UserId,
                request.ClassroomId,
                request.StartTime,
                request.EndTime,
                errorCode);

            throw new ContractOperationException(errorCode);
        }

        var booking = new BookingDto(
            Guid.NewGuid(),
            request.UserId,
            request.ClassroomId,
            request.StartTime,
            request.EndTime,
            request.Purpose,
            "Pending");

        _bookings.TryAdd(booking.Id, booking);

        _logger.LogInformation(
            "Booking created: {Operation} {Result} BookingId={BookingId} UserId={UserId} ClassroomId={ClassroomId} StartTime={StartTime} EndTime={EndTime}",
            "CreateBooking",
            "Success",
            booking.Id,
            booking.UserId,
            booking.ClassroomId,
            booking.StartTime,
            booking.EndTime);

        return booking;
    }

    public async Task<BookingDto> ApproveBookingAsync(
        ApproveBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_bookings.TryGetValue(request.BookingId, out var booking))
        {
            _logger.LogWarning(
                "Booking approval rejected: {Operation} {Result} BookingId={BookingId} AdminId={AdminId} ErrorCode={ErrorCode}",
                "ApproveBooking",
                "Rejected",
                request.BookingId,
                request.AdminId,
                "BOOKING_NOT_FOUND");

            throw new ContractOperationException("BOOKING_NOT_FOUND");
        }

        if (booking.Status != "Pending")
        {
            _logger.LogWarning(
                "Booking approval rejected: {Operation} {Result} BookingId={BookingId} AdminId={AdminId} ErrorCode={ErrorCode}",
                "ApproveBooking",
                "Rejected",
                request.BookingId,
                request.AdminId,
                "INVALID_BOOKING_STATUS");

            throw new ContractOperationException("INVALID_BOOKING_STATUS");
        }

        await _reservationService.ReserveSlotAsync(
            new ReserveSlotRequest(booking.Id, booking.ClassroomId, booking.StartTime, booking.EndTime),
            cancellationToken);

        var approved = booking with { Status = "Approved" };
        _bookings[approved.Id] = approved;

        _logger.LogInformation(
            "Booking approved: {Operation} {Result} BookingId={BookingId} AdminId={AdminId} ClassroomId={ClassroomId} StartTime={StartTime} EndTime={EndTime}",
            "ApproveBooking",
            "Success",
            approved.Id,
            request.AdminId,
            approved.ClassroomId,
            approved.StartTime,
            approved.EndTime);

        return approved;
    }

    public async Task<BookingDto> CancelBookingAsync(
        Guid bookingId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (!_bookings.TryGetValue(bookingId, out var booking))
        {
            _logger.LogWarning(
                "Booking cancellation rejected: {Operation} {Result} BookingId={BookingId} UserId={UserId} ErrorCode={ErrorCode}",
                "CancelBooking",
                "Rejected",
                bookingId,
                userId,
                "BOOKING_NOT_FOUND");

            throw new ContractOperationException("BOOKING_NOT_FOUND");
        }

        if (!isAdmin && booking.UserId != userId)
        {
            _logger.LogWarning(
                "Booking cancellation rejected: {Operation} {Result} BookingId={BookingId} UserId={UserId} ErrorCode={ErrorCode}",
                "CancelBooking",
                "Rejected",
                bookingId,
                userId,
                "FORBIDDEN");

            throw new ContractOperationException("FORBIDDEN");
        }

        if (booking.Status == "Cancelled")
        {
            return booking;
        }

        if (booking.Status == "Approved")
        {
            var releaseResult = await _releaseService.ReleaseSlotAsync(
                new ReleaseSlotRequest(booking.Id, booking.ClassroomId, booking.StartTime, booking.EndTime),
                cancellationToken);

            if (!releaseResult.Success && releaseResult.ErrorCode != "SLOT_NOT_RESERVED")
            {
                _logger.LogWarning(
                    "Booking cancellation rejected: {Operation} {Result} BookingId={BookingId} UserId={UserId} ErrorCode={ErrorCode}",
                    "CancelBooking",
                    "Rejected",
                    bookingId,
                    userId,
                    releaseResult.ErrorCode);

                throw new ContractOperationException(releaseResult.ErrorCode!);
            }
        }

        var cancelled = booking with { Status = "Cancelled" };
        _bookings[cancelled.Id] = cancelled;

        _logger.LogInformation(
            "Booking cancelled: {Operation} {Result} BookingId={BookingId} UserId={UserId} ClassroomId={ClassroomId} StartTime={StartTime} EndTime={EndTime}",
            "CancelBooking",
            "Success",
            cancelled.Id,
            userId,
            cancelled.ClassroomId,
            cancelled.StartTime,
            cancelled.EndTime);

        return cancelled;
    }
}