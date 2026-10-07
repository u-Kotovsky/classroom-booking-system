using ClassroomBooking.Bookings;
using ClassroomBooking.Contracts;
using ClassroomBooking.Schedule;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ClassroomBooking.IntegrationTests;

public class BookingScheduleIntegrationTests
{
    [Fact]
    public async Task CreateBooking_WhenSlotFree_ReturnsPendingAndLogsSuccess()
    {
        using var context = CreateContext();

        var request = new CreateBookingRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            "Учебное занятие");

        var booking = await context.BookingService.CreateBookingAsync(request);

        Assert.Equal("Pending", booking.Status);
        Assert.True(HasLog(context.Provider, LogLevel.Information, "CreateBooking", "Success"));
    }

    [Fact]
    public async Task CreateBooking_WhenSlotBusy_ReturnsScheduleConflictAndLogsWarning()
    {
        using var context = CreateContext();

        var classroomId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(1);

        var firstBooking = await context.BookingService.CreateBookingAsync(
            new CreateBookingRequest(Guid.NewGuid(), classroomId, startTime, endTime, "Первое занятие"));

        await context.BookingService.ApproveBookingAsync(
            new ApproveBookingRequest(firstBooking.Id, Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<ContractOperationException>(() =>
            context.BookingService.CreateBookingAsync(
                new CreateBookingRequest(
                    Guid.NewGuid(),
                    classroomId,
                    startTime.AddMinutes(30),
                    endTime.AddMinutes(30),
                    "Второе занятие")));

        Assert.Equal("SCHEDULE_CONFLICT", exception.ErrorCode);
        Assert.True(HasLog(context.Provider, LogLevel.Warning, "CheckAvailability", "Rejected", "SCHEDULE_CONFLICT"));
        Assert.True(HasLog(context.Provider, LogLevel.Warning, "CreateBooking", "Rejected", "SCHEDULE_CONFLICT"));
    }

    [Fact]
    public async Task ApproveBooking_ReservesSlotAndChangesStatusToApproved()
    {
        using var context = CreateContext();

        var booking = await context.BookingService.CreateBookingAsync(
            new CreateBookingRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                "Лабораторная работа"));

        var approved = await context.BookingService.ApproveBookingAsync(
            new ApproveBookingRequest(booking.Id, Guid.NewGuid()));

        Assert.Equal("Approved", approved.Status);
        Assert.True(HasLog(context.Provider, LogLevel.Information, "ReserveSlot", "Success"));
        Assert.True(HasLog(context.Provider, LogLevel.Information, "ApproveBooking", "Success"));
    }

    [Fact]
    public async Task CancelBooking_ReleasesSlotAndChangesStatusToCancelled()
    {
        using var context = CreateContext();

        var classroomId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddHours(1);

        var booking = await context.BookingService.CreateBookingAsync(
            new CreateBookingRequest(Guid.NewGuid(), classroomId, startTime, endTime, "Семинар"));

        await context.BookingService.ApproveBookingAsync(
            new ApproveBookingRequest(booking.Id, Guid.NewGuid()));

        var cancelled = await context.BookingService.CancelBookingAsync(
            booking.Id,
            booking.UserId,
            isAdmin: false);

        var availability = await context.ScheduleService.CheckAvailabilityAsync(
            new CheckAvailabilityRequest(classroomId, startTime, endTime));

        Assert.Equal("Cancelled", cancelled.Status);
        Assert.True(availability.IsAvailable);
        Assert.True(HasLog(context.Provider, LogLevel.Information, "ReleaseSlot", "Success"));
        Assert.True(HasLog(context.Provider, LogLevel.Information, "CancelBooking", "Success"));
    }

    private static TestContext CreateContext()
    {
        var provider = new CapturingLoggerProvider();
        var loggerFactory = new LoggerFactory();
        loggerFactory.AddProvider(provider);

        var scheduleService = new ScheduleService(loggerFactory.CreateLogger<ScheduleService>());
        var bookingService = new BookingService(
            scheduleService,
            scheduleService,
            scheduleService,
            loggerFactory.CreateLogger<BookingService>());

        return new TestContext(scheduleService, bookingService, provider, loggerFactory);
    }

    private static bool HasLog(
        CapturingLoggerProvider provider,
        LogLevel level,
        string operation,
        string result,
        string? errorCode = null)
    {
        return provider.Entries.Any(entry =>
            entry.Level == level &&
            GetString(entry.State, "Operation") == operation &&
            GetString(entry.State, "Result") == result &&
            (errorCode is null || GetString(entry.State, "ErrorCode") == errorCode));
    }

    private static string? GetString(IReadOnlyDictionary<string, object?> state, string key)
    {
        return state.TryGetValue(key, out var value) ? value as string : null;
    }

    private sealed class TestContext : IDisposable
    {
        public TestContext(
            ScheduleService scheduleService,
            BookingService bookingService,
            CapturingLoggerProvider provider,
            LoggerFactory loggerFactory)
        {
            ScheduleService = scheduleService;
            BookingService = bookingService;
            Provider = provider;
            LoggerFactory = loggerFactory;
        }

        public ScheduleService ScheduleService { get; }
        public BookingService BookingService { get; }
        public CapturingLoggerProvider Provider { get; }
        public LoggerFactory LoggerFactory { get; }

        public void Dispose()
        {
            LoggerFactory.Dispose();
        }
    }
}