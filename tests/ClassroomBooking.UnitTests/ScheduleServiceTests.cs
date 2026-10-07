using ClassroomBooking.Contracts;
using ClassroomBooking.Schedule;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClassroomBooking.UnitTests;

public class ScheduleServiceTests
{
    [Fact]
    public async Task CheckAvailabilityAsync_WhenNoOverlap_ReturnsAvailable()
    {
        var service = new ScheduleService(NullLogger<ScheduleService>.Instance);
        var classroomId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(1);

        var result = await service.CheckAvailabilityAsync(
            new CheckAvailabilityRequest(classroomId, startTime, endTime));

        Assert.True(result.IsAvailable);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_WhenEndTimeBeforeStartTime_ReturnsInvalidTimeInterval()
    {
        var service = new ScheduleService(NullLogger<ScheduleService>.Instance);
        var classroomId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = DateTime.UtcNow;

        var result = await service.CheckAvailabilityAsync(
            new CheckAvailabilityRequest(classroomId, startTime, endTime));

        Assert.False(result.IsAvailable);
        Assert.Equal("INVALID_TIME_INTERVAL", result.ErrorCode);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_WhenSlotReserved_ReturnsScheduleConflict()
    {
        var service = new ScheduleService(NullLogger<ScheduleService>.Instance);
        var classroomId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(1);

        await service.ReserveSlotAsync(
            new ReserveSlotRequest(bookingId, classroomId, startTime, endTime));

        var result = await service.CheckAvailabilityAsync(
            new CheckAvailabilityRequest(classroomId, startTime.AddMinutes(30), endTime.AddMinutes(30)));

        Assert.False(result.IsAvailable);
        Assert.Equal("SCHEDULE_CONFLICT", result.ErrorCode);
    }
}