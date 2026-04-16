using CKYC.Server.Data;
using Microsoft.EntityFrameworkCore;
using CKYC.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CkycDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CkycDb")));
builder.Services.Configure<SftpInboundOptions>(
    builder.Configuration.GetSection("SftpInbound"));
builder.Services.AddHostedService<SftpInboundPuller>();
builder.Services.AddHostedService<InboundZipProcessor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();