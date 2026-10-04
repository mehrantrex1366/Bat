namespace Bat.AspNetCore;

public class JwtToken
{
    public string Token { get; set; }
    public string TokenType { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpireTime { get; set; }

    public JwtToken() { }

    public JwtToken(SecurityTokenDescriptor securityTokenDescriptor)
    {
        var handler = new JwtSecurityTokenHandler();
        var securityToken = handler.CreateToken(securityTokenDescriptor);
        Token = handler.WriteToken(securityToken);
        TokenType = "Bearer";
        ExpireTime = securityToken.ValidTo.AddHours(3.5);
        RefreshToken = Randomizer.GetRandomString(32);
    }
}