using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace section3._2;

public class InitService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public InitService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        SalesContext salesDb = scope.ServiceProvider.GetRequiredService<SalesContext>();
        
        // TODO Section 3.2 Step 2
        // add cache invalidation logic here.
        IDistributedCache cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        // End Section 3.2 Step 2

        List<Task> cachePipe = new List<Task>
        {
            cache.RemoveAsync("top:sales", cancellationToken),
            cache.RemoveAsync("top:name", cancellationToken),
            cache.RemoveAsync("totalSales", cancellationToken)
        };
        cachePipe.AddRange(salesDb.Employees.Select(employee => cache.RemoveAsync($"employee:{employee.EmployeeId}:avg", cancellationToken)));

        await Task.WhenAll(cachePipe);
        // end cache invalidation logic
        
        await salesDb.Database.ExecuteSqlRawAsync("DELETE FROM Employees", cancellationToken);
        await salesDb.Database.ExecuteSqlRawAsync("DELETE FROM Sales",cancellationToken);

        string[] names = new[] { "Alice", "Bob", "Carlos", "Dan", "Yves" };
        Random random = new Random();
        foreach (string name in names)
        {   
            Employee employee = new Employee { Name = name };
            salesDb.Employees.Add(employee);
        }

        await salesDb.SaveChangesAsync(cancellationToken);

        foreach (string name in names)
        {
            Employee employee = salesDb.Employees.First(x => x.Name == name);
            for (int i = 0; i < 10000; i++)
            {
                employee.Sales.Add(new Sale(){Total = random.Next(1000,30000)});
            }
        }
        await salesDb.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}