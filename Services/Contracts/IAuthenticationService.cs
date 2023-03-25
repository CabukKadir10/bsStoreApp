using Entities.DataTransferObjects;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Contracts
{
    public interface IAuthenticationService
    {
        Task<IdentityResult> RegisterUser(UserForRegistrationDto userForRegistrationDto); // kullanıcı kaydetme
        Task<bool> ValideteUser(UserForAuthenticationDto userForAuthDto); //kullanıcıyı doğrulama
        Task<TokenDto> CreateToken(bool populateExp); // token oluşturma
        Task<TokenDto> RefreshToken(TokenDto tokenDto); // token güncelleme
    }
}
