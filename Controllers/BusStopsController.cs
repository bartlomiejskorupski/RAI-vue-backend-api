using backendASPNET.Data;
using backendASPNET.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace backendASPNET.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BusStopsController : Controller
{
    private readonly LocalContext _context;
    private readonly IMemoryCache _cache;
    private readonly string _allStopsCacheKey = "stops";
    private readonly string _allStopsUrl = @"https://ckan.multimediagdansk.pl/dataset/c24aa637-3619-4dc2-a171-a23eec8f2172/resource/4c4025f0-01bf-41f7-a39f-d156d201b82b/download/stops.json";
    private readonly string _departuresUrl = @"http://ckan2.multimediagdansk.pl/delays";

    public BusStopsController(LocalContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if(!_cache.TryGetValue(_allStopsCacheKey, out object? allStops))
        {
            await Console.Out.WriteLineAsync("NOT CACHED, CACHING...");
            allStops = await FetchUrl(_allStopsUrl, typeof(ZTMAllStopsResponse));
            var cacheEntryOpts = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.Now.AddMinutes(10),
                SlidingExpiration = TimeSpan.FromMinutes(2)
            };
            _cache.Set(_allStopsCacheKey, allStops, cacheEntryOpts);
        }
        await Console.Out.WriteLineAsync("RETURNING ALL STOPS");

        var allStopsRes = allStops as ZTMAllStopsResponse;

        return Ok(allStopsRes!.FirstOrDefault().Value);
    }

    [HttpGet("favorite")]
    public async Task<IActionResult> GetFavoriteStops()
    {
        var user = await GetUserFromClaim();

        if (user == null)
            return NotFound(new { Message = "wtf" });

        return Ok(user.FavoriteBusStops);
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddFavoriteStop(int stopId, string name)
    {
        var user = await GetUserFromClaim();

        if (user == null)
            return NotFound();

        var existing = user.FavoriteBusStops.Where(s => s.StopId == stopId).FirstOrDefault();
        if (existing is BusStop)
            return BadRequest();

        user.FavoriteBusStops.Add(new BusStop { Name = name, StopId = stopId });
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("favorite")]
    public async Task<IActionResult> DeleteFromFavorites(int stopId)
    {
        var user = await GetUserFromClaim();
        if (user == null) return NotFound();

        var removed = user.FavoriteBusStops.Where(bs => bs.StopId == stopId).FirstOrDefault();

        if (removed == null)
            return NotFound();

        _context.Remove(removed);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("departures")]
    public async Task<IActionResult> GetDepartures(int stopId)
    {
        return Ok(await FetchUrl(_departuresUrl + $"?stopId={stopId}"));
    }

    [NonAction]
    private async Task<User?> GetUserFromClaim()
    {
        var nameClaim = User.FindFirst(ClaimTypes.Name);
        await Console.Out.WriteLineAsync(nameClaim!.Value);
        var user = await _context.Users.Where(u => u.Login.Equals(nameClaim!.Value)).FirstOrDefaultAsync();
        return user;
    }

    [NonAction]
    private async Task<object?> FetchUrl(string url, Type? jsonType = null)
    {
        var client = new HttpClient();
        var resMsg = await client.GetAsync(url);
        return  await resMsg.Content.ReadFromJsonAsync(jsonType ?? typeof(object));
    }

}
