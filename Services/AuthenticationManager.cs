using AutoMapper;
using Entities.DataTransferObjects;
using Entities.Exceptions;
using Entities.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Services.Contracts;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class AuthenticationManager : IAuthenticationService
    {
        private readonly ILoggerService _logger;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        private User? _user; //kullanıcı bilgilerinin tutulacagı yer

        public AuthenticationManager(ILoggerService logger, 
            IMapper mapper, 
            UserManager<User> userManager, 
            IConfiguration configuration)
        {
            _logger = logger;
            _mapper = mapper;
            _userManager = userManager;
            _configuration = configuration;
        }

        //Güvenlik için token oluşturma
        public async Task<TokenDto> CreateToken(bool populateExp)
        {
            var singinCredentials = GetSiginCredentials(); // kullanıcı bilgilerini alma
            var claims = await GetClaims(); //hak, iddia, roller
            var tokenOptions = GenerateTokenOptions(singinCredentials, claims); // üretme

            var refreshToken = GenerateRefreshToken();
            _user.RefreshToken = refreshToken;

            if (populateExp)
                _user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);

            await _userManager.UpdateAsync(_user);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenOptions);

            return new TokenDto()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
            };
        }

        //Kullanıcıyı kaydetme işlemi
        public async Task<IdentityResult> RegisterUser(UserForRegistrationDto userForRegistrationDto)
        {                                 //Kullanıcı adı soyadı pasword maili nosu gibi özelliklerin tutuldugu yer.
            var user = _mapper.Map<User>(userForRegistrationDto);//dtoyu nesneye cevirip user değişkenine atıyoruz
                            
            var result = await _userManager
                .CreateAsync(user, userForRegistrationDto.Password); // kullanıcı adı soyadı ve şifresi oluşturulup sonuc değişkenşne atanır.

            if (result.Succeeded)
                await _userManager.AddToRolesAsync(user, userForRegistrationDto.Roles);
            //sonuc başarılı ise rol kısmına eklenir.
            
            return result;
        }

        //kullanıcı doğrulama fonksiyonu
        public async Task<bool> ValideteUser(UserForAuthenticationDto userForAuthDto)
        {
            _user = await _userManager.FindByNameAsync(userForAuthDto.UserName); //kullanıcı ismini usera atadık

            var result = (_user != null && await _userManager.CheckPasswordAsync(_user, userForAuthDto.Password));
            //sonrasında atama işlemi olmuşmu ve şifreler eşleşip eşleşmediğini kontrol ediyoruz.
            if (!result)// hata varsa hata mesaji döndürüyoruz
                _logger.LogWarning($"{nameof(ValideteUser)} : Authentication failed. Wrong userName or password");

            return result; // hata yoksa kullanıcı doğrulama işlemi true döner.
        }

        private SigningCredentials GetSiginCredentials()
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["secretKey"]);
            var secret = new SymmetricSecurityKey(key);
            return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
        }

        private async Task<List<Claim>> GetClaims()
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, _user.UserName)// username bilgisini alıyoruz
            };

            var roles = await _userManager.GetRolesAsync(_user); //rolleri alıyoruz. list string şeklinde alıyoruz.

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role)); // alınan her bir rolü claime cevirip listemize ekliyoruz
            }

            return claims;
        }

        private JwtSecurityToken GenerateTokenOptions(SigningCredentials singinCredentials, List<Claim> claims)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");

            var tokenOptions = new JwtSecurityToken( //nesne ürettik
                    issuer: jwtSettings["validIssuer"],
                    audience: jwtSettings["validAudience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["expires"])),
                    signingCredentials: singinCredentials);
            return tokenOptions;
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);

                return Convert.ToBase64String(randomNumber);
            }
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["secretKey"];

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["validIssuer"],
                ValidAudience = jwtSettings["validAudience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;

            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);

            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if(jwtSecurityToken is null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }

        public async Task<TokenDto> RefreshToken(TokenDto tokenDto)
        {
            var principal = GetPrincipalFromExpiredToken(tokenDto.AccessToken);
            var user = await _userManager.FindByNameAsync(principal.Identity.Name);

            if (user is null || user.RefreshToken != tokenDto.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
                throw new RefreshTokenBadRequestException();

            _user = user;
            return await CreateToken(populateExp: false);
        }
    }
}
