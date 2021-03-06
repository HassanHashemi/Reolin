﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reolin.Web.Api.ViewModels;
using Reolin.Web.Security.Membership.Core;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Options;
using Reolin.Web.Security.Jwt;
using Reolin.Web.Api.Infra.mvc;
using System.Net;

namespace Reolin.Web.Api.Controllers
{
    public class AccountController : BaseController
    {
        private readonly IUserSecurityManager _userManager;
        private readonly IOptions<TokenProviderOptions> _tokenOptionsWrapper;
        private readonly IJWTManager _jwtManager;

        public AccountController(IUserSecurityManager userManager, IOptions<TokenProviderOptions> options, IJWTManager jwtManager)
        {
            this._userManager = userManager;
            this._jwtManager = jwtManager;
            this._tokenOptionsWrapper = options;
        }

        private TokenProviderOptions Options
        {
            get
            {
                return _tokenOptionsWrapper.Value;
            }
        }

        private IUserSecurityManager UserManager
        {
            get
            {
                return _userManager;
            }
        }

        private IJWTManager JwtManager
        {
            get
            {
                return _jwtManager;
            }
        }

        [HttpPost]
        [AllowAnonymous]

        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return BadRequest(this.ModelState);
            }
            
            try
            {
                await this.UserManager.CreateAsync(model.UserName, model.Password, model.Email);
                return Ok(new { model.Email, model.UserName });
            }
            catch (Exception ex) when (ex is IdentityException)
            {
                this.ModelState.AddModelError("identificationError", ex.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            IdentityResult result = (await this.UserManager.GetLoginInfo(model.UserName, model.Password));
            if (!result.Succeeded)
            {
                return WriteError(result);
            }

            this.Options.Claims = GetPerUserClaims(result.User.UserName, result.User.Id, result.User.Roles.Select(r => r.Name));

            return Ok(new
            {
                access_token = this.JwtManager.IssueJwt(this.Options),
                expires_in = Options.Expiration
            });
        }

        private IActionResult WriteError(IdentityResult result)
        {
            switch (result.Error)
            {
                case IdentityResultErrors.EmptyOrUnknown:
                    return Error("Something went wrong.");

                case IdentityResultErrors.UserNotFound:
                    return Error(HttpStatusCode.NotFound, "Specified User could not be found");

                case IdentityResultErrors.InvalidPassowrd:
                    return Error(HttpStatusCode.BadRequest, "Invalid password" );

                default:
                    return BadRequest(result.Exception.Message);
            }
        }

        private List<Claim> GetPerUserClaims(string userName, int userId, IEnumerable<string> roles)
        {
            const string roleClaimName = "roles";
            return new List<Claim>()
                   {
                        new Claim(JwtRegisteredClaimNames.Sub, userName),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToString(), ClaimValueTypes.Integer64),
                        new Claim(roleClaimName, GetRoleString(roles), "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"),
                        new Claim("Id", userId.ToString(), "http://www.w3.org/2001/XMLSchema#integer")
                   };
        }

        private string GetRoleString(IEnumerable<string> roles)
        {
            StringBuilder sb = new StringBuilder();
            roles.ForEach(r => sb.Append($"{r},"));
            return sb.ToString().Remove(sb.Length - 1);
        }
    }
}
