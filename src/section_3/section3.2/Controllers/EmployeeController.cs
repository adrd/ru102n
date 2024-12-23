using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace section3._2.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class EmployeeController : ControllerBase
{
    private readonly SalesContext _salesDb;
    private readonly IDistributedCache _cache;

    public EmployeeController(SalesContext salesDb, IDistributedCache cache)
    {
        _cache = cache;
        _salesDb = salesDb;
    }

    [HttpGet("all")]
    public IEnumerable<Employee> GetEmployees()
    {
        return _salesDb.Employees;
    }

    [HttpGet("top")]
    public async Task<Dictionary<string,object>> GetTopSalesperson()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        // TODO Section 3.2 step 4
        // add cache check here
        Task<string?> topSalesTask = _cache.GetStringAsync("top:sales");
        Task<string?> topNameTask = _cache.GetStringAsync("top:name");

        await Task.WhenAll(topSalesTask, topNameTask);

        if (!string.IsNullOrEmpty(topSalesTask.Result) && !string.IsNullOrEmpty(topNameTask.Result))
        {
            stopwatch.Stop();
            return new Dictionary<string, object>()
            {
                { "sum_sales", topSalesTask.Result },
                { "employee_name", topNameTask.Result },
                { "time", stopwatch.ElapsedMilliseconds }
            };
        }
        // end Section 3.2 step 4

        var topSalesperson = await _salesDb.Employees.Select(x=>new {Employee = x, sumSales = x.Sales
            .Sum(x=>x.Total)}).OrderByDescending(x=>x.sumSales)
            .FirstAsync();
        stopwatch.Stop();

        // TODO Section 3.2 step 3
        // add cache insert here
        DistributedCacheEntryOptions cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
        Task topSalesInsertTask = _cache.SetStringAsync("top:sales", topSalesperson.sumSales.ToString(), cacheOptions);
        Task topNameInsertTask = _cache.SetStringAsync("top:name", topSalesperson.Employee.Name, cacheOptions);
        await Task.WhenAll(topSalesInsertTask, topNameInsertTask);
        // End Section 3.2 step 3

        return new Dictionary<string, object>()
        {
            { "sum_sales", topSalesperson.sumSales },
            { "employee_name", topSalesperson.Employee.Name },
            { "time", stopwatch.ElapsedMilliseconds }
        };
    }

    [HttpGet("average/{id}")]
    public async Task<Dictionary<string,double>> GetAverage([FromRoute] int id)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        // TODO Section 3.2 step 5
        // add caching logic here
        string key = $"employee:{id}:avg";
        string? cacheResult = await _cache.GetStringAsync(key);

        if (cacheResult != null)
        {
            stopwatch.Stop();
            return new Dictionary<string, double>
            {
                { "average", double.Parse(cacheResult) },
                { "elapsed", stopwatch.ElapsedMilliseconds }
            };
        }
        // end Section 3.2 step 5

        double avg = await _salesDb.Employees.Include(x => x.Sales).Where(x=>x.EmployeeId == id).Select(x=>x.Sales.Average(y=>y.Total)).FirstAsync();
        
        // TODO Section 3.2 step 6
        // add cache set here
        await _cache.SetStringAsync(key, avg.ToString(CultureInfo.InvariantCulture), options: new DistributedCacheEntryOptions{SlidingExpiration = TimeSpan.FromMinutes(30)});
        // end Section 3.2 step 6

        stopwatch.Stop();
        return new Dictionary<string, double>
        {
            { "average", avg },
            { "elapsed", stopwatch.ElapsedMilliseconds }
        };
    }

    [HttpGet("totalSales")]
    public async Task<Dictionary<string, long>> GetTotalSales()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        
        // TODO Section 3.2 step 7
        // add caching logic here
        string? cacheResult = await _cache.GetStringAsync("totalSales");
        if (cacheResult != null)
        {
            stopwatch.Stop();
            return new Dictionary<string, long>()
            {
                { "Total Sales", long.Parse(cacheResult) },
                { "elapsed", stopwatch.ElapsedMilliseconds }
            };
        }
        // end Section 3.2 step 7

        int totalSales = await _salesDb.Sales.SumAsync(x => x.Total);

        // TODO Section 3.2 step 8
        // add cache set here
        await _cache.SetStringAsync("totalSales", totalSales.ToString(CultureInfo.InvariantCulture), new DistributedCacheEntryOptions(){AbsoluteExpiration = DateTime.Today.AddDays(1)});
        // end Section 3.2 step 8

        stopwatch.Stop();
        return new Dictionary<string, long>()
        {
            { "Total Sales", totalSales },
            { "elapsed", stopwatch.ElapsedMilliseconds }
        }; 
    }
}