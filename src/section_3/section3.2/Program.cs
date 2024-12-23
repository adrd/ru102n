using Newtonsoft.Json;
using section3._2;
using StackExchange.Redis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson(x=>x.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<SalesContext>();
// !!! ATTENTION - BE AWARE - Comment this in order to migrate database and uncomment in order to run app !!!
builder.Services.AddHostedService<InitService>();

// TODO Section 3.2 Step 1
// call AddStackExchangeRedisCache here.
builder.Services.AddStackExchangeRedisCache(x => x.ConfigurationOptions = new ConfigurationOptions
{
    EndPoints = { "localhost:6379" },
    Password = ""
});
// End Section 3.2 Step 1
WebApplication app = builder.Build();

// DIRTY HACK, we WILL come back to fix this - Uncomment in order to migrate database
//IServiceScope scope = app.Services.CreateScope();
//SalesContext context = scope.ServiceProvider.GetRequiredService<SalesContext>();
//context.Database.EnsureDeleted();
//context.Database.EnsureCreated();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
