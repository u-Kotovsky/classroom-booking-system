using System.Collections.Concurrent;
using ClassroomBooking.Contracts;
using Microsoft.Extensions.Logging;

namespace ClassroomBooking.Schedule;

public sealed class ScheduleService :
    IScheduleConflictService,
    IScheduleReservationService,
    IScheduleReleaseService
{
    private readonly ILogger<ScheduleService> _logger;
    private readonly ConcurrentDictionary<Guid, ScheduleSlot> _slots = new();
    private readonly ConcurrentDictionary<Guid, Guid> _bookingSlotMap = new();
    private readonly ConcurrentDictionary<Guid, byte> _releasedBookings = new();

    public ScheduleService(ILogger<ScheduleService> logger)
    {
        _logger = logger;
    }

    public Task<CheckAvailabilityResult> CheckAvailabilityAsync(
        CheckAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EndTime <= request.StartTime)
        {
            _logger.LogWarning(
                "Schedule availability rejected: {Operation} {Result} ClassroomId={ClassroomId} StartTime={StartTime} EndTime={EndTime} ErrorCode={ErrorCode}",
                "CheckAvailability",
                "Rejected",
                request.ClassroomId,
                request.StartTime,
                request.EndTime,
                "INVALID_TIME_INTERVAL");

            return Task.FromResult(new CheckAvailabilityResult(false, "INVALID_TIME_INTERVAL"));
        }

        var conflict = _slots.Values.FirstOrDefault(slot =>
            slot.ClassroomId == request.ClassroomId &&
            slot.Status != "Free" &&
            Overlaps(slot.StartTime, slot.EndTime, request.StartTime, request.EndTime));

        if (conflict is not null)
        {
            _logger.LogWarning(
                "Schedule availability rejected: {Operation} {Result} ClassroomId={ClassroomId} StartTime={StartTime} EndTime={EndTime} ErrorCode={ErrorCode}",
                "CheckAvailability",
                "Rejected",
                request.ClassroomId,
                request.StartTime,
                request.EndTime,
                "SCHEDULE_CONFLICT");

            return Task.FromResult(new CheckAvailabilityResult(false, "SCHEDULE_CONFLICT"));
        }

        _logger.LogInformation(
            "Schedule availability checked: {Operation} {Result} ClassroomId={ClassroomId} StartTime={StartTime} EndTime={EndTime}",
            "CheckAvailability",
            "Allowed",
            request.ClassroomId,
            request.StartTime,
            request.EndTime);

        return Task.FromResult(new CheckAvailabilityResult(true, null));
    }

    public Task<ScheduleSlotDto> ReserveSlotAsync(
        ReserveSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EndTime <= request.StartTime)
        {
            _logger.LogWarning(
                "Schedule reservation rejected: {Operation} {Result} BookingId={BookingId} ClassroomId={ClassroomId} StartTime={StartTime} EndTime={EndTime} ErrorCode={ErrorCode}",
                "ReserveSlot",
                "Rejected",
                request.BookingId,
                request.ClassroomId,
                request.StartTime,
                request.EndTime,
                "INVALID_TIME_INTERVAL");

            throw new ContractOperationException("INVALID_TIME_INTERVAL");
        }

        if (_bookingSlotMap.TryGetValue(request.BookingId, out var existingSlotId) &&
            _slots.TryGetValue(existingSlotId, out var existingSlot))
        {
            _logger.LogInformation(
                "Schedule reservation reused: {Operation} {Result} BookingId={BookingId} SlotId={SlotId} ClassroomId={ClassroomId} StartTime={StartTime} EndTime={EndTime}",
                "ReserveSlot",
                "Success",
                request.BookingId,
                existingSlot.Id,
                request.ClassroomId,
                request.StartTime,
                request.EndTime);

            return Task.FromResult(existingSlot.ToDto());
        }

        var conflict = _slots.Values.FirstOrDefault(slot =>
            slot.ClassroomId == request.ClassroomId &&
            slot.Status != "Free" &&
            Overlaps(slot.StartTime, slot.EndTime, request.StartTime, request.EndTime));

        if (conflict is not null)
        {
            _logger.LogWarning(
                "Schedule reservation rejected: {Operation} {Result} BookingId={BookingId} ClassroomId={ClassroomId} StartTime={StartTime} EndTime={EndTime} ErrorCode={ErrorCode}",
                "ReserveSlot",
                "Rejected",
                request.BookingId,
                request.ClassroomId,
                request.StartTime,
                request.EndTime,
                "SLOT_ALREADY_RESERVED");

            throw new ContractOperationException("SLOT_ALREADY_RESERVED");
        }

        var slot = new ScheduleSlot
        {
            Id = Guid.NewGuid(),
            BookingId = request.BookingId,
            ClassroomId = request.ClassroomId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = "Booked"
        };

        _slots.TryAdd(slot.Id, slot);
        _bookingSlotMap.TryAdd(request.BookingId, slot.Id);

        _logger.LogInformation(
            "Schedule reservation created: {Operation} {Result} BookingId={BookingId} SlotId={SlotId} ClassroomId={ClassroomId} StartTime={StartTime} EndTime={EndTime}",
            "ReserveSlot",
            "Success",
            request.BookingId,
            slot.Id,
            request.ClassroomId,
            request.StartTime,
            request.EndTime);

        return Task.FromResult(slot.ToDto());
    }

    public Task<ReleaseSlotResult> ReleaseSlotAsync(
        ReleaseSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EndTime <= request.StartTime)
        {
            _logger.LogWarning(
                "Schedule release rejected: {Operation} {Result} BookingId={BookingId} ClassroomId={ClassroomId} StartTime={StartTime} EndTime={EndTime} ErrorCode={ErrorCode}",
                "ReleaseSlot",
                "Rejected",
                request.BookingId,
                request.ClassroomId,
                request.StartTime,
                request.EndTime,
                "INVALID_TIME_INTERVAL");

            return Task.FromResult(new ReleaseSlotResult(false, "INVALID_TIME_INTERVAL"));
        }

        if (_releasedBookings.ContainsKey(request.BookingId))
        {
            _logger.LogInformation(
                "Schedule release idempotent: {Operation} {Result} BookingId={BookingId} ClassroomId={ClassroomId} StartTime={StartTime} EndTime={EndTime}",
                "ReleaseSlot",
                "Success",
                request.BookingId,
                request.ClassroomId,
                request.StartTime,
                request.EndTime);

            return Task.FromResult(new ReleaseSlotResult(true, null));
        }

        if (!_bookingSlotMap.TryRemove(request.BookingId, out var slotId))
        {
            _logger.LogWarning(
                "Schedule release rejected: {Operation} {Result} BookingId={BookingId} ClassroomId={ClassroomId} StartTime={StartTime} EndTime={EndTime} ErrorCode={ErrorCode}",
                "ReleaseSlot",
                "Rejected",
                request.BookingId,
                request.ClassroomId,
                request.StartTime,
                request.EndTime,
                "SLOT_NOT_RESERVED");

            return Task.FromResult(new ReleaseSlotResult(false, "SLOT_NOT_RESERVED"));
        }

        if (_slots.TryGetValue(slotId, out var slot))
        {
            slot.Status = "Free";
            slot.BookingId = null;
        }

        _releasedBookings.TryAdd(request.BookingId, 0);

        _logger.LogInformation(
            "Schedule slot released: {Operation} {Result} BookingId={BookingId} SlotId={SlotId} ClassroomId={ClassroomId} StartTime={StartTime} EndTime={EndTime}",
            "ReleaseSlot",
            "Success",
            request.BookingId,
            slotId,
            request.ClassroomId,
            request.StartTime,
            request.EndTime);

        return Task.FromResult(new ReleaseSlotResult(true, null));
    }

    private static bool Overlaps(DateTime leftStart, DateTime leftEnd, DateTime rightStart, DateTime rightEnd)
    {
        return leftStart < rightEnd && rightStart < leftEnd;
    }

    private sealed class ScheduleSlot
    {
        public Guid Id { get; init; }
        public Guid? BookingId { get; set; }
        public Guid ClassroomId { get; init; }
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }
        public string Status { get; set; } = "Free";

        public ScheduleSlotDto ToDto()
        {
            return new ScheduleSlotDto(Id, ClassroomId, StartTime, EndTime, Status);
        }
    }
}