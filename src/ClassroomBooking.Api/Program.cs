using ClassroomBooking.Bookings;
using ClassroomBooking.Contracts;
using ClassroomBooking.Schedule;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();
builder.Services.AddControllers();

builder.Services.AddSingleton<ScheduleService>();
builder.Services.AddSingleton<IScheduleConflictService>(sp => sp.GetRequiredService<ScheduleService>());
builder.Services.AddSingleton<IScheduleReservationService>(sp => sp.GetRequiredService<ScheduleService>());
builder.Services.AddSingleton<IScheduleReleaseService>(sp => sp.GetRequiredService<ScheduleService>());
builder.Services.AddSingleton<IBookingService, BookingService>();

var app = builder.Build();

app.MapControllers();

app.Run();