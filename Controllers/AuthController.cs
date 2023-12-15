using backendASPNET.Data;
using backendASPNET.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace backendASPNET.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly LocalContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(LocalContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRequest req)
    {
        if(_context.Users.Any(u => u.Login == req.Login))
        {
            return BadRequest(new { Message = "User with given login already exists" });
        }

        _context.Users.Add(new User { Login = req.Login, Password = req.Password });
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "User successfully created!"
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest req)
    {
        var foundUser = await _context.Users.Where(u => u.Login == req.Login).FirstOrDefaultAsync();

        if (foundUser is null)
        {
            return Unauthorized(new { Message = "User with given login does not exist." });
        }
        if(!foundUser.Password.Equals(req.Password)) 
        {
            return Unauthorized(new { Message = "Incorrect password." });
        }

        var token = GenerateToken(foundUser);

        return Ok(new
        {
            Token = token
        });
    }

    [NonAction]
    private string GenerateToken(User user) 
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"]!,
            _configuration["Jwt:Audience"]!,
            new[] {  new Claim(ClaimTypes.Name, user.Login) }, 
            expires: DateTime.Now.AddMinutes(120),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
