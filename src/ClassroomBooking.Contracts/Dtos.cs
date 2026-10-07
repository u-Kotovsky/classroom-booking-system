namespace ClassroomBooking.Contracts;

public record ClassroomDto(
    Guid Id,
    string Name,
    string Building,
    int Capacity,
    string Type,
    string Status);

public record CreateBookingRequest(
    Guid UserId,
    Guid ClassroomId,
    DateTime StartTime,
    DateTime EndTime,
    string Purpose);

public record BookingDto(
    Guid Id,
    Guid UserId,
    Guid ClassroomId,
    DateTime StartTime,
    DateTime EndTime,
    string Purpose,
    string Status);

public record CheckAvailabilityRequest(
    Guid ClassroomId,
    DateTime StartTime,
    DateTime EndTime);

public record CheckAvailabilityResult(
    bool IsAvailable,
    string? ErrorCode);

public record ApproveBookingRequest(
    Guid BookingId,
    Guid AdminId);

public record ReserveSlotRequest(
    Guid BookingId,
    Guid ClassroomId,
    DateTime StartTime,
    DateTime EndTime);

public record ScheduleSlotDto(
    Guid Id,
    Guid ClassroomId,
    DateTime StartTime,
    DateTime EndTime,
    string Status);

public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role);

public record ReleaseSlotRequest(
    Guid BookingId,
    Guid ClassroomId,
    DateTime StartTime,
    DateTime EndTime);

public record ReleaseSlotResult(
    bool Success,
    string? ErrorCode);