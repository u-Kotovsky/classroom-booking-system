namespace ClassroomBooking.Contracts;

public interface IScheduleConflictService
{
    Task<CheckAvailabilityResult> CheckAvailabilityAsync(
        CheckAvailabilityRequest request,
        CancellationToken cancellationToken = default);
}

public interface IScheduleReservationService
{
    Task<ScheduleSlotDto> ReserveSlotAsync(
        ReserveSlotRequest request,
        CancellationToken cancellationToken = default);
}

public interface IScheduleReleaseService
{
    Task<ReleaseSlotResult> ReleaseSlotAsync(
        ReleaseSlotRequest request,
        CancellationToken cancellationToken = default);
}

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken = default);

    Task<BookingDto> ApproveBookingAsync(
        ApproveBookingRequest request,
        CancellationToken cancellationToken = default);

    Task<BookingDto> CancelBookingAsync(
        Guid bookingId,
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}